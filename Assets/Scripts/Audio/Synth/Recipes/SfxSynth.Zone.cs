using System;
using System.Collections.Generic;

namespace CloneZ
{
    // Zone gear, anomalies, emissions and machines. Each sound is built from physical layers — a transient, a resonant
    // body, a texture and its space — and every loop is periodic by construction (tones snapped to whole cycles, events
    // wrapped round the end, noise filtered round the seam), so none of them needs a cross-fade.
    public sealed partial class SfxSynth
    {
        partial void RegisterZone()
        {
            // Peaks are set against the originals' perceived (A-weighted) loudness, so the call sites keep their volumes.
            // ── Zone gear ───────────────────────────────────────────────────────────────────────────
            // A handheld detector's piezo pip (also the breach charge's timer beep).
            Add(SfxIds.DetectorBeep, g => ZoneSfx.DetectorBeep(g), 2).Peak(0.45f).Priority(40);
            // The anomaly chirp: a harsher, buzzing double note falling a minor third.
            Add(SfxIds.DetectorAlert, g => ZoneSfx.DetectorAlert(g), 2).Peak(0.6f).Priority(40);
            // A Geiger counter's tick: eight different ones, so a fast count never machine-guns.
            Add(SfxIds.Geiger, g => ZoneSfx.GeigerTick(g), 8).Peak(0.45f).Priority(100);
            // A revealed artifact's faint glassy song (pure partials are loud for their level: a low peak).
            Add(SfxIds.ArtifactHum, g => ZoneSfx.ArtifactHum(g), 1, BedRate).Loop().Peak(0.12f).Priority(110).Spread(30f);

            // ── Anomalies: what each kind gives off while it waits (Anomalies.cs plays them all at one volume) ───
            Add(SfxIds.AnomalyHum, g => ZoneSfx.PsiHum(g), 1, BedRate).Loop().Peak(0.6f).Priority(100).Spread(40f);
            Add(SfxIds.AnomalyHumGravity, g => ZoneSfx.GravityHum(g), 1, BedRate).Loop().Peak(0.55f).Priority(100).Spread(40f);
            Add(SfxIds.AnomalyHumFire, g => ZoneSfx.FireHum(g), 1, BedRate).Loop().Peak(0.3f).Priority(100).Spread(40f);
            Add(SfxIds.AnomalyHumElectric, g => ZoneSfx.ElectricHum(g), 1, BedRate).Loop().Peak(0.2f).Priority(100).Spread(40f);
            Add(SfxIds.AnomalyHumAcid, g => ZoneSfx.AcidHum(g), 1, BedRate).Loop().Peak(0.28f).Priority(100).Spread(40f);

            // ── Anomalies: discharges ───────────────────────────────────────────────────────────────
            Add(SfxIds.AnomalyGravity, g => ZoneSfx.Gravity(g), 3).Peak(0.9f).Priority(50);
            Add(SfxIds.AnomalyFire, g => ZoneSfx.Fire(g), 3).Peak(0.55f).Priority(50);
            Add(SfxIds.AnomalyElectric, g => ZoneSfx.Electric(g), 4).Priority(50);
            Add(SfxIds.AnomalyCrackle, g => ZoneSfx.Crackle(g), 4).Peak(0.9f).Priority(60);
            Add(SfxIds.AnomalyAcid, g => ZoneSfx.Acid(g), 3).Priority(60);
            Add(SfxIds.AnomalyTrigger, g => ZoneSfx.Trigger(g), 2).Priority(45);

            // ── Emissions: heard across the whole Zone (flat, so stereo) ────────────────────────────
            AddStereo(SfxIds.EmissionSiren, g => ZoneSfx.Siren(g), 1, BedRate).Loop().Peak(0.9f).Priority(8).Dry();
            AddStereo(SfxIds.EmissionRumble, g => ZoneSfx.Rumble(g), 3, BedRate).Peak(0.85f).Priority(20).Dry();
            AddStereo(SfxIds.EmissionWave, g => ZoneSfx.Wave(g), 1, BedRate).Peak(0.9f).Priority(4).Dry().Duck(-9f, 60f, 2.5f, 5f);

            // ── Machines ────────────────────────────────────────────────────────────────────────────
            Add(SfxIds.HeliRotor, g => ZoneSfx.Rotor(g), 1, BedRate).Loop().Peak(0.8f).Priority(20).Range(250f).Doppler(1f).Spread(60f).Reverb(0.5f);
            Add(SfxIds.FlareBurn, g => ZoneSfx.Flare(g), 1, BedRate).Loop().Peak(0.6f).Priority(90);
            Add(SfxIds.EngineLoop, g => ZoneSfx.EngineIdle(g), 1, BedRate).Loop().Peak(0.6f).Priority(30).Doppler(0.7f).Spread(20f);
            Add(SfxIds.EngineStart, g => ZoneSfx.EngineStart(g), 2, BedRate).Priority(30);
            Add(SfxIds.EngineStartFail, g => ZoneSfx.EngineStartFail(g), 2, BedRate).Peak(0.95f).Priority(40);
            Add(SfxIds.EngineStop, g => ZoneSfx.EngineStop(g), 2, BedRate).Priority(40);
            Add(SfxIds.CarDoor, g => ZoneSfx.CarDoor(g), 3).Priority(70);
            Add(SfxIds.CarSwitch, g => ZoneSfx.CarSwitch(g), 3).Peak(0.6f).Priority(120);
            Add(SfxIds.TyreBurst, g => ZoneSfx.TyreBurst(g), 2).Peak(0.9f).Priority(30);
            Add(SfxIds.DroneBuzz, g => ZoneSfx.Quad(g), 1, BedRate).Loop().Peak(0.42f).Priority(50).Doppler(0.6f);
            Add(SfxIds.DroneRover, g => ZoneSfx.Rover(g), 1, BedRate).Loop().Peak(0.5f).Priority(90).Doppler(0.3f);
        }

        // The Zone family's own toolbox and recipes.
        private static class ZoneSfx
        {
            #region Tools
            private const int TabSize = 4096;
            private static readonly float[] s_Sin = BuildSin();

            private static float[] BuildSin()
            {
                var t = new float[TabSize + 2];
                for (int i = 0; i < t.Length; i++) t[i] = MathF.Sin(Dsp.TwoPi * i / TabSize);
                return t;
            }

            /// <summary>One cycle of a band-limited wave from harmonic amplitudes (index 0 = the fundamental).</summary>
            internal static float[] Table(float[] amps, float[] phases = null)
            {
                var t = new float[TabSize + 2];
                for (int h = 0; h < amps.Length; h++)
                {
                    float a = amps[h];
                    if (a == 0f) continue;
                    long k = h + 1, off = phases != null ? (long)(phases[h] * TabSize) : 0L;
                    for (int i = 0; i < t.Length; i++) t[i] += a * s_Sin[(int)((k * i + off) % TabSize)];
                }
                return t;
            }

            /// <summary>A wave table read at <paramref name="phase"/> cycles (0..1), interpolated.</summary>
            internal static float Read(float[] table, double phase)
            {
                float p = (float)(phase * TabSize);
                int i = (int)p;
                if (i > TabSize) i = TabSize;
                return table[i] + (table[i + 1] - table[i]) * (p - i);
            }

            /// <summary>sin(2π·phase) from the table, for any phase.</summary>
            internal static float Sin(double phase) => Read(s_Sin, phase - Math.Floor(phase));

            /// <summary>f(t seconds) evaluated every <paramref name="step"/> samples and interpolated: cheap control curves.</summary>
            internal static float[] Curve(int n, int rate, Func<float, float> f, int step = 32)
            {
                var y = new float[n];
                float prev = f(0f);
                for (int k = 0; k < n; k += step)
                {
                    int end = Math.Min(n, k + step);
                    float next = f(end / (float)rate);
                    float inv = 1f / (end - k);
                    for (int i = k; i < end; i++) y[i] = prev + (next - prev) * (i - k) * inv;
                    prev = next;
                }
                return y;
            }

            /// <summary>Adds a sine at <paramref name="hz"/>, snapped to whole cycles over the buffer so a loop wraps.</summary>
            internal static void Partial(float[] x, int rate, float hz, float gain, float[] amp = null, double phase = 0)
            {
                int n = x.Length;
                double step = Math.Max(1.0, Math.Round(hz * (double)n / rate)) / n * TabSize, ph = (phase - Math.Floor(phase)) * TabSize;
                float[] t = s_Sin;
                for (int i = 0; i < n; i++)
                {
                    int j = (int)ph;
                    float f = (float)(ph - j);
                    float v = t[j] + (t[j + 1] - t[j]) * f;
                    x[i] += amp != null ? gain * amp[i] * v : gain * v;
                    ph += step;
                    if (ph >= TabSize) ph -= TabSize;
                }
            }

            /// <summary>Adds a tone reading <paramref name="table"/> along the frequency curve <paramref name="hz"/>; for a loop
            /// (<paramref name="wrap"/>) the curve is nudged to whole cycles so the tone flows across the seam.</summary>
            internal static void Osc(float[] x, int rate, float[] table, float[] hz, float gain, float[] amp, bool wrap, double phase = 0)
            {
                int n = x.Length;
                double k = 1.0;
                if (wrap)
                {
                    double total = 0;
                    for (int i = 0; i < n; i++) total += hz[i];
                    total /= rate;
                    k = Math.Max(1.0, Math.Round(total)) / Math.Max(1e-9, total);
                }
                double ph = (phase - Math.Floor(phase)) * TabSize, inv = k / rate * TabSize;
                for (int i = 0; i < n; i++)
                {
                    int j = (int)ph;
                    float f = (float)(ph - j);
                    float v = table[j] + (table[j + 1] - table[j]) * f;
                    x[i] += amp != null ? gain * amp[i] * v : gain * v;
                    ph += hz[i] * inv;
                    while (ph >= TabSize) ph -= TabSize;
                }
            }

            /// <summary>A one-pole low-pass run twice round a loop, so its state wraps the seam.</summary>
            internal static void LoopLP1(float[] x, float hz, int rate)
            {
                OnePole f = OnePole.At(hz, rate);
                for (int i = 0; i < x.Length; i++) f.Lp(x[i]);
                for (int i = 0; i < x.Length; i++) x[i] = f.Lp(x[i]);
            }

            /// <summary>A band-pass (or low-pass) whose cutoff follows <paramref name="hz"/>[i], run twice round a loop.</summary>
            internal static void LoopSweep(float[] x, int rate, float q, float[] hz, bool lowPass)
            {
                var f = new Biquad();
                var y = new float[x.Length];
                for (int pass = 0; pass < 2; pass++)
                    for (int i = 0; i < x.Length; i++)
                    {
                        if ((i & 31) == 0)
                        {
                            if (lowPass) f.SetLP(hz[i], q, rate);
                            else f.SetBP(hz[i], q, rate);
                        }
                        y[i] = f.Run(x[i]);
                    }
                Array.Copy(y, x, x.Length);
            }

            /// <summary>A smooth random curve round a loop, -1..1 (noise low-passed twice at <paramref name="hz"/>, wrapping).</summary>
            internal static float[] LoopWander(SynthGen g, int n, float hz)
            {
                float[] w = Dsp.White(g, n);
                LoopLP1(w, hz, g.Rate);
                LoopLP1(w, hz, g.Rate);
                Dsp.RemoveMean(w);
                Dsp.Norm(w);
                return w;
            }

            /// <summary>Pink noise that wraps round a loop (its filter runs twice).</summary>
            internal static float[] LoopPink(SynthGen g, int n)
            {
                float[] w = Dsp.White(g, n);
                var x = new float[n];
                float b0 = 0f, b1 = 0f, b2 = 0f;
                for (int pass = 0; pass < 2; pass++)
                    for (int i = 0; i < n; i++)
                    {
                        float v = w[i];
                        b0 = 0.99765f * b0 + v * 0.0990460f;
                        b1 = 0.96300f * b1 + v * 0.2965164f;
                        b2 = 0.57000f * b2 + v * 1.0526913f;
                        x[i] = (b0 + b1 + b2 + v * 0.1848f) * 0.22f;
                    }
                return x;
            }

            /// <summary>Brown noise that wraps round a loop, unit peak.</summary>
            internal static float[] LoopBrown(SynthGen g, int n)
            {
                float[] w = Dsp.White(g, n);
                var x = new float[n];
                float z = 0f;
                for (int pass = 0; pass < 2; pass++)
                    for (int i = 0; i < n; i++)
                    {
                        z = z * 0.985f + w[i] * 0.12f;
                        x[i] = z;
                    }
                Dsp.Norm(x);
                return x;
            }

            /// <summary>A spark or a hard tick: a two-sample kick, three short high modes and a wisp of noise.</summary>
            internal static float[] Spark(SynthGen g, float ringHz, float tau)
            {
                int rate = g.Rate;
                var k = new float[Math.Max(16, (int)(tau * 7f * rate) + 8)];
                float pol = g.Chance(0.5f) ? 1f : -1f;
                k[1] = pol;
                k[2] = -pol * g.Range(0.3f, 0.7f);
                k[3] = pol * g.Range(0f, 0.25f);
                Dsp.Modes(k, rate, 0f, 0.55f * pol, new[] { ringHz, ringHz * g.Range(1.45f, 1.75f), ringHz * g.Range(2.3f, 2.9f) },
                    new[] { 1f, 0.55f, 0.3f }, new[] { tau, tau * 0.7f, tau * 0.45f });
                float nt = Math.Max(0.0002f, tau * 0.4f) * rate;
                for (int i = 1; i < k.Length; i++) k[i] += 0.3f * g.Bipolar() * MathF.Exp(-i / nt);
                return k;
            }

            /// <summary>A frying crackle: single-sample ticks at <paramref name="perSecond"/> × dens[i], most tiny, a few large
            /// (<paramref name="sharp"/> skews them small). Filter it afterwards.</summary>
            internal static void Fry(float[] x, SynthGen g, float perSecond, float[] dens, float sharp)
            {
                float p = perSecond / g.Rate;
                for (int i = 0; i < x.Length; i++)
                {
                    float d = dens != null ? dens[i] : 1f;
                    if (g.Next() < p * d) x[i] += (g.Chance(0.5f) ? 1f : -1f) * MathF.Pow(g.Next(), sharp);
                }
            }

            /// <summary>A few dispersed reflections of what is already in x (later, duller, quieter): space without a comb.</summary>
            internal static void Reflect(float[] x, SynthGen g, int count, float first, float spread, float gain, float lpHz)
            {
                int rate = g.Rate;
                var src = (float[])x.Clone();
                Dsp.LP(src, lpHz, rate);
                for (int k = 0; k < count; k++)
                {
                    float d = first + spread * (k + g.Next()) / count;
                    float a = gain * MathF.Pow(0.72f, k) * g.Range(0.7f, 1f);
                    Dsp.Mix(x, src, (int)(d * rate), g.Chance(0.5f) ? a : -a);
                }
            }

            /// <summary>Fades x smoothly to silence between <paramref name="from"/> and <paramref name="to"/> seconds.</summary>
            internal static void FadeTail(float[] x, int rate, float from, float to)
            {
                int a = Math.Max(0, (int)(from * rate)), b = Math.Max(a + 1, (int)(to * rate));
                for (int i = a; i < x.Length; i++) x[i] *= i >= b ? 0f : 1f - Dsp.SmoothStep((i - a) / (float)(b - a));
            }

            /// <summary>A cheap <see cref="Dsp.Thump"/> for long tails: a sine gliding from hz0 toward hz1 under an attack and an
            /// exponential decay, run on recurrences and the sine table instead of per-sample transcendentals.</summary>
            internal static void Thud(float[] x, int rate, float at, float gain, float hz0, float hz1, float glideTau, float attack, float tau)
            {
                int start = (int)(at * rate), len = (int)((attack + tau * 7f) * rate), na = Math.Max(1, (int)(attack * rate));
                float decay = MathF.Exp(-1f / (tau * rate)), glide = MathF.Exp(-1f / (glideTau * rate)), e = 1f, gl = 1f;
                double ph = 0, scale = (double)TabSize / rate;
                float[] tab = s_Sin;
                for (int i = 0; i < len; i++)
                {
                    int k = start + i;
                    if (k >= x.Length) break;
                    float a = i < na ? i / (float)na : (e *= decay);
                    ph += (hz1 + (hz0 - hz1) * gl) * scale;
                    gl *= glide;
                    if (ph >= TabSize) ph -= TabSize;
                    int j = (int)ph;
                    if (k >= 0) x[k] += gain * a * (tab[j] + (tab[j + 1] - tab[j]) * (float)(ph - j));
                }
            }

            /// <summary>tanh, near enough (a Padé fit, exact at ±3 and flat beyond): a cheap soft clip.</summary>
            internal static float Soft(float v)
            {
                if (v >= 3f) return 1f;
                if (v <= -3f) return -1f;
                float v2 = v * v;
                return v * (27f + v2) / (27f + 9f * v2);
            }

            /// <summary>Soft-limits a spiky sound (normalise, then tanh): the snaps stay sharp but the body comes up under them.</summary>
            internal static void Squash(float[] x, float drive)
            {
                Dsp.Norm(x);
                for (int i = 0; i < x.Length; i++) x[i] = Soft(x[i] * drive);
            }

            /// <summary>The stereo <see cref="Squash(float[], float)"/>: both sides normalised together, then limited.</summary>
            internal static void Squash(StereoBuf s, float drive)
            {
                float p = Math.Max(Dsp.Peak(s.L), Dsp.Peak(s.R));
                if (p < 1e-9f) return;
                float k = drive / p;
                for (int i = 0; i < s.Length; i++)
                {
                    s.L[i] = Soft(s.L[i] * k);
                    s.R[i] = Soft(s.R[i] * k);
                }
            }

            /// <summary>Asymmetric saturation (unit-peak in, tanh with a bias) from sample <paramref name="from"/> on: a sub-bass
            /// tone grows the harmonics small speakers can play. Leaves a little DC — high-pass afterwards.</summary>
            internal static void Grit(float[] x, float drive, float bias, int from = 0)
            {
                Dsp.Norm(x);
                float b = Soft(bias);
                for (int i = Math.Max(0, from); i < x.Length; i++) x[i] = Soft(x[i] * drive + bias) - b;
            }

            /// <summary>
            /// A light stereo space for distance: the mid diffused through two allpasses into four damped, cross-fed delay
            /// lines (a Householder feedback network), left and right read from different lines — far cheaper than the core's
            /// reverb and plenty for the open air. For a loop it first runs over the last <paramref name="warm"/> seconds so
            /// its tail wraps the seam. The wet sound is scaled to <paramref name="wet"/> × the dry mid's RMS.
            /// </summary>
            internal static void Space(StereoBuf s, int rate, float rt60, float dampHz, float wet, float size, bool loop, float warm = 3f)
            {
                int n = s.Length;
                var mid = new float[n];
                for (int i = 0; i < n; i++) mid[i] = 0.5f * (s.L[i] + s.R[i]);
                Diffuse(mid, (int)(0.0047f * rate), 0.6f, loop);
                Diffuse(mid, (int)(0.0113f * rate), 0.6f, loop);
                int d0 = Math.Max(8, (int)(0.0531f * size * rate)), d1 = Math.Max(8, (int)(0.0677f * size * rate));
                int d2 = Math.Max(8, (int)(0.0797f * size * rate)), d3 = Math.Max(8, (int)(0.0941f * size * rate));
                var b0 = new float[d0];
                var b1 = new float[d1];
                var b2 = new float[d2];
                var b3 = new float[d3];
                float g0 = MathF.Pow(10f, -3f * d0 / (rt60 * rate)), g1 = MathF.Pow(10f, -3f * d1 / (rt60 * rate));
                float g2 = MathF.Pow(10f, -3f * d2 / (rt60 * rate)), g3 = MathF.Pow(10f, -3f * d3 / (rt60 * rate));
                float a = MathF.Exp(-Dsp.TwoPi * dampHz / rate);
                float z0 = 0f, z1 = 0f, z2 = 0f, z3 = 0f;
                int i0 = 0, i1 = 0, i2 = 0, i3 = 0;
                var wl = new float[n];
                var wr = new float[n];
                int from = Math.Max(0, n - (int)(warm * rate));
                for (int pass = loop ? 0 : 1; pass < 2; pass++)
                {
                    for (int i = pass == 0 ? from : 0; i < n; i++)
                    {
                        float o0 = b0[i0], o1 = b1[i1], o2 = b2[i2], o3 = b3[i3];
                        z0 = o0 + (z0 - o0) * a;
                        z1 = o1 + (z1 - o1) * a;
                        z2 = o2 + (z2 - o2) * a;
                        z3 = o3 + (z3 - o3) * a;
                        float y0 = z0 * g0, y1 = z1 * g1, y2 = z2 * g2, y3 = z3 * g3;
                        float h = 0.5f * (y0 + y1 + y2 + y3), inp = mid[i];
                        b0[i0] = y0 - h + inp;
                        b1[i1] = y1 - h + inp;
                        b2[i2] = y2 - h - inp;
                        b3[i3] = y3 - h - inp;
                        if (++i0 >= d0) i0 = 0;
                        if (++i1 >= d1) i1 = 0;
                        if (++i2 >= d2) i2 = 0;
                        if (++i3 >= d3) i3 = 0;
                        if (pass == 1)
                        {
                            wl[i] = o0 - o2;
                            wr[i] = o1 - o3;
                        }
                    }
                }
                double sd = 0, sw = 0;
                for (int i = 0; i < n; i++)
                {
                    float m = 0.5f * (s.L[i] + s.R[i]);
                    sd += m * m;
                    sw += 0.5 * (wl[i] * wl[i] + wr[i] * wr[i]);
                }
                float k = sw > 1e-18 ? (float)(wet * Math.Sqrt(sd / sw)) : 0f;
                for (int i = 0; i < n; i++)
                {
                    s.L[i] += wl[i] * k;
                    s.R[i] += wr[i] * k;
                }
            }

            // A Schroeder allpass (delay d, coefficient k) in place; for a loop it first runs over the loop's last 40 delays
            // (by then its memory of anything earlier has died away) so it wraps the seam.
            private static void Diffuse(float[] x, int d, float k, bool loop)
            {
                var line = new float[Math.Max(1, d)];
                float[] src = loop ? (float[])x.Clone() : x;
                int j = 0, warmFrom = Math.Max(0, x.Length - line.Length * 40);
                for (int pass = loop ? 0 : 1; pass < 2; pass++)
                    for (int i = pass == 0 ? warmFrom : 0; i < x.Length; i++)
                    {
                        float bo = line[j];
                        float v = src[i] + k * bo;
                        line[j] = v;
                        if (++j >= line.Length) j = 0;
                        if (pass == 1) x[i] = bo - k * v;
                    }
            }
            #endregion

            #region Gear
            // A handheld detector's pip: the piezo disc rung by a square drive.
            internal static float[] DetectorBeep(SynthGen g)
            {
                float[] x = g.Buffer(0.1f);
                Piezo(x, g, 0.0025f, 0.055f, 2200f * (1f + g.Bipolar() * 0.003f), 1f, 0f, 0f);
                return x;
            }

            // The anomaly chirp: two buzzing notes, the second a minor third down — harsher than the artifact pip.
            internal static float[] DetectorAlert(SynthGen g)
            {
                float[] x = g.Buffer(0.22f);
                float f = 1600f * (1f + g.Bipolar() * 0.003f);
                Piezo(x, g, 0.0025f, 0.05f, f, 1f, 72f, 0.65f);
                Piezo(x, g, 0.092f, 0.07f, f * 0.84f, 1f, 72f, 0.65f);
                return x;
            }

            // One note of a piezo sounder: a square drive ringing the disc's resonance (it swells in over a millisecond and
            // rings on a moment after the drive stops), a hard little case, a cheap DAC's grit, optionally chopped by a buzz.
            private static void Piezo(float[] x, SynthGen g, float at, float len, float hz, float gain, float buzzHz, float buzzDepth)
            {
                int rate = g.Rate;
                int n = (int)((len + 0.015f) * rate);
                var drive = new float[n];
                double ph = 0, bph = 0;
                float dt = hz / rate;
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)rate;
                    float env = t < len ? Math.Min(1f, t / 0.0008f) : Math.Max(0f, 1f - (t - len) / 0.0015f);
                    float s = Dsp.Square((float)ph, dt);
                    if (buzzDepth > 0f)
                    {
                        s *= 1f - buzzDepth * Dsp.Clamp01(0.5f + 1.6f * Sin(bph));
                        bph += buzzHz / rate;
                    }
                    drive[i] = s * env;
                    ph += dt;
                    if (ph >= 1.0) ph -= 1.0;
                }
                var disc = (float[])drive.Clone();
                Dsp.BP(disc, hz * 1.02f, 5f, rate);
                Dsp.BP(disc, hz * 1.02f, 2f, rate);
                Dsp.Norm(disc);
                var y = new float[n];
                float held = 0f;
                for (int i = 0; i < n; i++)
                {
                    float v = 0.8f * disc[i] + 0.2f * drive[i];
                    if (i % 3 == 0) held = v;
                    y[i] = 0.85f * v + 0.15f * held;
                }
                Dsp.HP(y, 600f, rate);
                Dsp.PeakEq(y, 4800f, 1.2f, 3f, rate);
                Dsp.LP(y, 10000f, rate);
                Dsp.Mix(x, y, (int)(at * rate), gain);
            }

            // A Geiger counter's tick: the tube's discharge kicking the little speaker — a click with nothing below 500 Hz,
            // the cone and case ringing for a millisecond or two — and every tick a little different.
            internal static float[] GeigerTick(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(0.03f);
                int at = (int)(0.0022f * rate) + g.Int(0, 8);
                float pol = g.Chance(0.5f) ? 1f : -1f;
                var k = new float[x.Length];
                k[at] = pol;
                k[at + 1] = pol * g.Range(0.2f, 0.7f);
                k[at + 2] = -pol * g.Range(0.2f, 0.6f);
                k[at + 3] = -pol * g.Range(0f, 0.3f);
                Dsp.HP(k, 500f, rate);
                Dsp.Mix(x, k, 0, 1f);
                float t0 = at / (float)rate, cone = g.Range(1900f, 2700f);
                Dsp.Modes(x, rate, t0, pol * g.Range(0.3f, 0.45f),
                    new[] { cone, cone * g.Range(1.55f, 1.8f), cone * g.Range(2.6f, 3.2f), g.Range(8000f, 10500f) },
                    new[] { 1f, g.Range(0.4f, 0.8f), g.Range(0.2f, 0.5f), g.Range(0.1f, 0.3f) },
                    new[] { g.Range(0.0008f, 0.0016f), 0.0007f, 0.0004f, 0.0002f });
                // The metal case: a faint ring a few milliseconds long.
                Dsp.Modes(x, rate, t0, g.Range(0.03f, 0.07f), new[] { g.Range(4600f, 7400f) }, new[] { 1f }, new[] { g.Range(0.003f, 0.006f) });
                return x;
            }

            // A revealed artifact: a glassy chord of a free bar's inharmonic modes, each beating slowly against a twin,
            // over a soft low drone and a breath of shimmer.
            internal static float[] ArtifactHum(SynthGen g)
            {
                int rate = g.Rate;
                const float L = 6f;
                int n = g.N(L);
                var x = new float[n];
                float b = g.Range(420f, 520f);
                float[] ratios = { 1f, 2.76f, 5.40f, 8.93f };
                float[] amps = { 1f, 0.45f, 0.2f, 0.08f };
                for (int k = 0; k < ratios.Length; k++)
                {
                    float[] a = Curve(n, rate, Dsp.Gusts(g, L, 1, 3, 2));
                    float beat = g.Int(1, 4) / L;
                    Partial(x, rate, b * ratios[k], amps[k] * 0.5f, a, g.Next());
                    Partial(x, rate, b * ratios[k] + beat, amps[k] * 0.4f, a, g.Next());
                }
                float[] slow = Curve(n, rate, Dsp.Gusts(g, L, 1, 2, 2));
                Partial(x, rate, b * 0.25f, 0.16f, slow);
                Partial(x, rate, b * 0.25f + 1f / 3f, 0.1f);
                float[] sh = Dsp.White(g, n);
                Dsp.LoopBP(sh, 6200f, 3f, rate);
                Dsp.Norm(sh);
                float[] w = LoopWander(g, n, 0.8f);
                for (int i = 0; i < n; i++) x[i] += sh[i] * 0.025f * (0.5f + 0.5f * w[i]);
                Dsp.LoopHP(x, 60f, rate);
                return x;
            }
            #endregion

            #region Anomaly hums
            // The psi kind (and any other): a low cluster beating slowly against itself with tritones in it that never
            // resolve, a glassy pair fluttering high above, a ghostly choir of breath through drifting vowel formants, and air
            // pushed through something that isn't there.
            internal static float[] PsiHum(SynthGen g)
            {
                int rate = g.Rate;
                const float L = 8f;
                int n = g.N(L);
                var x = new float[n];
                float[] breathe = Curve(n, rate, Dsp.Gusts(g, L, 1, 3, 3));
                float[] sway = Curve(n, rate, Dsp.Gusts(g, L, 2, 5, 3));
                float r = g.Range(0.97f, 1.03f);
                Partial(x, rate, 55f * r, 0.3f, breathe);
                Partial(x, rate, 55f * r + 0.375f, 0.24f, sway, 0.3);
                Partial(x, rate, 77.8f * r, 0.13f, sway, 0.6);
                Partial(x, rate, 82.4f * r, 0.13f, breathe, 0.1);
                Partial(x, rate, 110.5f * r, 0.2f, sway, 0.2);
                Partial(x, rate, 110.5f * r + 0.25f, 0.16f, breathe, 0.7);
                Partial(x, rate, 155.6f * r, 0.12f, breathe, 0.4);
                Partial(x, rate, 164.6f * r, 0.1f, sway, 0.8);
                Partial(x, rate, 220.4f * r, 0.08f, sway, 0.5);
                Partial(x, rate, 221f * r, 0.07f, breathe, 0.9);
                Partial(x, rate, 311.1f * r, 0.05f, sway, 0.15);
                float[] glint = Curve(n, rate, Dsp.Gusts(g, L, 1, 4, 2));
                for (int i = 0; i < n; i++) glint[i] = glint[i] * glint[i] * glint[i];
                Partial(x, rate, 1244f * r, 0.03f, glint);
                Partial(x, rate, 1244f * r + 6.5f, 0.03f, glint, 0.5);
                Partial(x, rate, 1867f * r, 0.012f, glint);
                float[] vowel = Curve(n, rate, Dsp.Gusts(g, L, 1, 2, 2));
                float[] choir = LoopPink(g, n);
                var c2 = (float[])choir.Clone();
                var f1 = new float[n];
                var f2 = new float[n];
                for (int i = 0; i < n; i++)
                {
                    f1[i] = 300f + 420f * vowel[i];
                    f2[i] = 870f + 330f * vowel[i];
                }
                LoopSweep(choir, rate, 9f, f1, false);
                LoopSweep(c2, rate, 11f, f2, false);
                Dsp.Norm(choir);
                Dsp.Norm(c2);
                for (int i = 0; i < n; i++) x[i] += (0.6f * choir[i] + 0.4f * c2[i]) * 0.16f * (0.3f + 0.7f * sway[i]);
                float[] air = LoopPink(g, n);
                float[] centre = Curve(n, rate, Dsp.Gusts(g, L, 1, 3, 2));
                for (int i = 0; i < n; i++) centre[i] = 160f * MathF.Pow(3f, centre[i]);
                LoopSweep(air, rate, 3f, centre, false);
                Dsp.Norm(air);
                for (int i = 0; i < n; i++) x[i] += air[i] * 0.24f * (0.4f + 0.6f * breathe[i]);
                Dsp.LoopHP(x, 30f, rate);
                return x;
            }

            // The sinkhole: the ground under strain — a sub-bass drone that wobbles as if the air were being bent, two swirls
            // of air dragged round and round, a low roll, and grit trickling in now and then.
            internal static float[] GravityHum(SynthGen g)
            {
                int rate = g.Rate;
                const float L = 6f;
                int n = g.N(L);
                var x = new float[n];
                float b = g.Range(34f, 39f);
                float[] f1 = Curve(n, rate, t => b * (1f + 0.025f * MathF.Sin(Dsp.TwoPi * 4f * t / L) + 0.01f * MathF.Sin(Dsp.TwoPi * 7f * t / L + 1.3f)));
                float[] f2 = new float[n], f3 = new float[n];
                for (int i = 0; i < n; i++)
                {
                    f2[i] = f1[i] * 2f + 0.25f;
                    f3[i] = f1[i] * 3f + 0.375f;
                }
                float[] press = Curve(n, rate, Dsp.Gusts(g, L, 2, 4, 3));
                Osc(x, rate, s_Sin, f1, 0.5f, press, true);
                Partial(x, rate, b + 0.25f, 0.3f);
                Osc(x, rate, s_Sin, f2, 0.26f, press, true, 0.25);
                Osc(x, rate, s_Sin, f3, 0.12f, press, true, 0.6);
                Swirl(x, g, L, 5, 260f, 2.2f, 0.3f);
                Swirl(x, g, L, 3, 150f, 1.6f, 0.25f);
                float[] rum = LoopBrown(g, n);
                Dsp.LoopLP(rum, 90f, rate);
                Dsp.Norm(rum);
                for (int i = 0; i < n; i++) x[i] += rum[i] * 0.25f * (0.5f + 0.5f * press[i]);
                var grit = new float[n];
                int trickles = g.Int(3, 6);
                for (int k = 0; k < trickles; k++)
                {
                    int centreAt = g.Int(0, n);
                    float dur = g.Range(0.3f, 0.9f);
                    int count = g.Int(15, 45);
                    for (int j = 0; j < count; j++)
                    {
                        float u = g.Next();
                        Dsp.MixWrap(grit, Spark(g, g.Range(1800f, 3800f), g.Range(0.0003f, 0.0009f)), centreAt + (int)(u * dur * rate), g.Range(0.2f, 1f) * (1f - 0.7f * u));
                    }
                }
                Dsp.LoopHP(grit, 700f, rate);
                Dsp.LoopLP(grit, 4000f, rate);
                Dsp.Norm(grit);
                for (int i = 0; i < n; i++) x[i] += grit[i] * 0.07f;
                Dsp.LoopHP(x, 24f, rate);
                return x;
            }

            // A swirl: noise through a band-pass whose centre circles 'cycles' times round the loop, louder as it rises.
            private static void Swirl(float[] x, SynthGen g, float loop, int cycles, float baseHz, float q, float gain)
            {
                int rate = g.Rate, n = x.Length;
                float[] s = LoopPink(g, n);
                float ph0 = g.Next();
                float[] u = Curve(n, rate, t => 0.5f + 0.5f * MathF.Sin(Dsp.TwoPi * (cycles * t / loop + ph0)));
                float[] c = new float[n];
                for (int i = 0; i < n; i++) c[i] = baseHz * (1f + 3f * u[i] * u[i]);
                LoopSweep(s, rate, q, c, false);
                Dsp.Norm(s);
                for (int i = 0; i < n; i++) x[i] += s[i] * gain * (0.25f + 0.75f * u[i] * u[i]);
            }

            // The embermoss: a smoulder that breathes — a low flickering roar, a mid rush, the hiss of hot gas, and embers
            // ticking and now and then snapping.
            internal static float[] FireHum(SynthGen g)
            {
                int rate = g.Rate;
                const float L = 6f;
                int n = g.N(L);
                var x = new float[n];
                float[] flick = LoopWander(g, n, 2.5f), fast = LoopWander(g, n, 11f);
                float[] roar = LoopBrown(g, n);
                Dsp.LoopLP(roar, 380f, rate);
                Dsp.Norm(roar);
                float[] body = LoopPink(g, n);
                Dsp.LoopBP(body, 650f, 0.8f, rate);
                Dsp.Norm(body);
                float[] hiss = Dsp.White(g, n);
                Dsp.LoopHP(hiss, 2600f, rate);
                Dsp.LoopLP(hiss, 7500f, rate);
                Dsp.Norm(hiss);
                for (int i = 0; i < n; i++)
                    x[i] = roar[i] * 0.55f * (0.65f + 0.25f * flick[i] + 0.1f * fast[i])
                        + body[i] * 0.3f * (0.6f + 0.4f * fast[i])
                        + hiss[i] * 0.06f * (0.6f + 0.4f * fast[i]);
                var cr = new float[n];
                int pops = (int)(L * g.Range(5f, 8f));
                for (int k = 0; k < pops; k++)
                    Dsp.MixWrap(cr, Spark(g, g.Range(1500f, 4200f), g.Range(0.0004f, 0.0012f)), g.Int(0, n), MathF.Pow(g.Next(), 2.2f));
                int snaps = g.Int(2, 4);
                for (int k = 0; k < snaps; k++)
                {
                    int at = g.Int(0, n);
                    Dsp.MixWrap(cr, Spark(g, g.Range(1200f, 2600f), g.Range(0.0012f, 0.0025f)), at, g.Range(1.2f, 1.8f));
                    Dsp.MixWrap(cr, Spark(g, g.Range(2500f, 4000f), 0.0006f), at + g.N(g.Range(0.004f, 0.015f)), 0.6f);
                }
                Dsp.LoopHP(cr, 900f, rate);
                Dsp.Norm(cr);
                for (int i = 0; i < n; i++) x[i] += cr[i] * 0.35f;
                Dsp.LoopHP(x, 30f, rate);
                return x;
            }

            // The staticbloom: a transformer's hum (the iron singing at twice the mains), corona frying on the voltage peaks,
            // and now and then an arc spitting across.
            internal static float[] ElectricHum(SynthGen g)
            {
                int rate = g.Rate;
                const float L = 6f;
                int n = g.N(L);
                var x = new float[n];
                const int H = 60;
                var amps = new float[H];
                var phases = new float[H];
                for (int k = 1; k <= H; k++)
                {
                    float f = 50f * k;
                    float a = k == 1 ? 0.12f : k % 2 == 0 ? 1f / MathF.Pow(k * 0.5f, 1.05f) : 0.06f / k;
                    float df1 = (f - 600f) / 180f, df2 = (f - 1700f) / 300f;
                    a *= 1f + 1.5f * MathF.Exp(-df1 * df1) + 0.8f * MathF.Exp(-df2 * df2);
                    if (f > 4500f) a *= 0.4f;
                    amps[k - 1] = a;
                    phases[k - 1] = g.Next();
                }
                float[] tab = Table(amps, phases);
                Dsp.Norm(tab);
                float[] load = Curve(n, rate, Dsp.Gusts(g, L, 1, 4, 3));
                double step = Math.Round(50.0 * n / rate) / n, ph = 0;
                var dens = new float[n];
                for (int i = 0; i < n; i++)
                {
                    x[i] = Read(tab, ph) * 0.5f * (0.75f + 0.25f * load[i]);
                    float s = Read(s_Sin, ph);
                    s *= s;
                    dens[i] = s * s * s * (0.4f + 0.6f * load[i]);
                    ph += step;
                    if (ph >= 1.0) ph -= 1.0;
                }
                var cor = new float[n];
                Fry(cor, g, 2600f, dens, 3f);
                Dsp.LoopHP(cor, 2400f, rate);
                Dsp.LoopLP(cor, 9000f, rate);
                Dsp.Norm(cor);
                for (int i = 0; i < n; i++) x[i] += cor[i] * 0.22f;
                int arcs = g.Int(2, 5);
                for (int a = 0; a < arcs; a++)
                {
                    var arc = new float[g.N(0.12f)];
                    int snaps = g.Int(3, 8);
                    for (int s = 0; s < snaps; s++)
                        Dsp.Mix(arc, Spark(g, g.Range(2500f, 6000f), g.Range(0.0003f, 0.0008f)), g.Int(0, arc.Length * 2 / 3), g.Range(0.3f, 1f));
                    double bp = 0;
                    for (int i = 0; i < arc.Length; i++)
                    {
                        arc[i] += 0.3f * Dsp.Saw((float)bp, 100f / rate) * Dsp.AD(i / (float)rate, 0.002f, 0.03f);
                        bp += 100.0 / rate;
                        if (bp >= 1.0) bp -= 1.0;
                    }
                    Dsp.MixWrap(x, arc, g.Int(0, n), g.Range(0.25f, 0.45f));
                }
                Dsp.LoopHP(x, 40f, rate);
                return x;
            }

            // The grey syrup: a faint fizz of tiny bubbles bursting, bubbles rising and popping, now and then a slow fat one,
            // over a low simmer.
            internal static float[] AcidHum(SynthGen g)
            {
                int rate = g.Rate;
                const float L = 6f;
                int n = g.N(L);
                var x = new float[n];
                float[] fw = LoopWander(g, n, 1.5f);
                var dens = new float[n];
                for (int i = 0; i < n; i++) dens[i] = 0.6f + 0.4f * fw[i];
                var fizz = new float[n];
                Fry(fizz, g, 3500f, dens, 4f);
                Dsp.LoopHP(fizz, 3800f, rate);
                Dsp.LoopLP(fizz, 11000f, rate);
                Dsp.Norm(fizz);
                for (int i = 0; i < n; i++) x[i] += fizz[i] * 0.3f;
                int count = (int)(L * g.Range(3f, 5f));
                for (int k = 0; k < count; k++)
                    Dsp.MixWrap(x, Dsp.Bubble(rate, g.Range(260f, 1100f), g.Range(0.015f, 0.05f), g.Range(0.5f, 2.2f)), g.Int(0, n), g.Range(0.04f, 0.16f));
                int fat = g.Int(3, 6);
                for (int k = 0; k < fat; k++)
                    Dsp.MixWrap(x, Dsp.Bubble(rate, g.Range(95f, 190f), g.Range(0.09f, 0.16f), g.Range(0.5f, 1f)), g.Int(0, n), g.Range(0.14f, 0.24f));
                float[] sim = LoopBrown(g, n);
                Dsp.LoopLP(sim, 220f, rate);
                Dsp.Norm(sim);
                for (int i = 0; i < n; i++) x[i] += sim[i] * 0.26f * (0.6f + 0.4f * fw[i]);
                Squash(x, 1.8f);
                Dsp.LoopHP(x, 35f, rate);
                return x;
            }
            #endregion

            #region Anomaly discharges
            // The sinkhole slams: air dragged inward rising to the hit, a deep pitched thump and the crack of the soil, then
            // the air rushing back out, dirt pattering down and a low roll.
            internal static float[] Gravity(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(2.4f);
                int n = x.Length;
                float hit = g.Range(0.28f, 0.36f), top = g.Range(1000f, 1500f);
                float[] pull = Dsp.Pink(g, n);
                Dsp.SweepBP(pull, rate, 1.4f, t => 130f * MathF.Pow(top / 130f, Dsp.Clamp01(t / hit)));
                Dsp.Shape(pull, rate, t => t < hit ? (0.35f + 0.65f * MathF.Pow(t / hit, 2f)) * Math.Min(1f, t / 0.012f) : MathF.Exp(-(t - hit) / 0.04f));
                Dsp.Norm(pull);
                Dsp.Mix(x, pull, 0, 0.9f);
                // A low vacuum moan falling under the pull.
                double ph = 0;
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)rate;
                    if (t > hit + 0.1f) break;
                    float u = Dsp.Clamp01(t / hit);
                    float a = t < hit ? u * u : MathF.Exp(-(t - hit) / 0.02f);
                    x[i] += 0.22f * a * Sin(ph);
                    ph += Dsp.Lerp(170f, 60f, u) / rate;
                }
                Dsp.Thump(x, rate, hit, 1f, g.Range(78f, 92f), 32f, 0.06f, 0.003f, 0.3f);
                Dsp.AddBurst(x, g, hit, 0.55f, 0.002f, 0.07f, 380f);
                Dsp.AddBurst(x, g, hit, 0.5f, 0.0004f, 0.02f, 4000f, 250f);
                var crunch = new float[n];
                Dsp.Grains(crunch, g, hit, hit + 0.45f, 110, 1f, 0.0009f, u => 1f - u);
                Dsp.HP(crunch, 500f, rate);
                Dsp.LP(crunch, 5000f, rate);
                Dsp.Norm(crunch);
                Dsp.Mix(x, crunch, 0, 0.35f);
                float[] back = Dsp.White(g, n);
                Dsp.SweepBP(back, rate, 1.1f, t => t < hit ? 900f : 900f * MathF.Pow(160f / 900f, Dsp.Clamp01((t - hit) / 0.7f)));
                Dsp.Shape(back, rate, t => Dsp.AD(t - hit - 0.02f, 0.03f, 0.25f));
                Dsp.Norm(back);
                Dsp.Mix(x, back, 0, 0.3f);
                var patter = new float[n];
                Dsp.Grains(patter, g, hit + 0.2f, 2.1f, 60, 1f, 0.0006f, u => (1f - u) * (1f - u));
                Dsp.HP(patter, 1200f, rate);
                Dsp.LP(patter, 7000f, rate);
                Dsp.Norm(patter);
                Dsp.Mix(x, patter, 0, 0.09f);
                float[] rum = Dsp.Brown(g, n);
                Dsp.LP(rum, 110f, rate);
                Dsp.Shape(rum, rate, t => Dsp.AD(t - hit, 0.04f, 0.55f));
                Dsp.Norm(rum);
                Dsp.Mix(x, rum, 0, 0.35f);
                Dsp.HP(x, 26f, rate);
                FadeTail(x, rate, 2.1f, 2.38f);
                return x;
            }

            // The embermoss flares: a vapour cloud catching all at once ("whoomp"), a ragged turbulent roar that brightens
            // while it's fiercest, and sparks spitting through it and after.
            internal static float[] Fire(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(2.3f);
                int n = x.Length;
                float hold = g.Range(0.25f, 0.45f), decay = g.Range(0.35f, 0.55f);
                Func<float, float> flame = t => t <= 0f ? 0f : t < 0.035f ? t / 0.035f : t < 0.035f + hold ? 1f : MathF.Exp(-(t - 0.035f - hold) / decay);
                Dsp.Thump(x, rate, 0.004f, 0.8f, g.Range(115f, 145f), 48f, 0.05f, 0.006f, 0.11f);
                Dsp.AddBurst(x, g, 0.004f, 0.5f, 0.004f, 0.06f, 700f);
                float[] roar = Dsp.White(g, n);
                float[] turb = Dsp.Wander(g, n, rate, 22f), slow = Dsp.Wander(g, n, rate, 5f);
                for (int i = 0; i < n; i++) roar[i] *= 0.35f + 0.45f * turb[i] + 0.2f * slow[i];
                Dsp.SweepLP(roar, rate, 0.8f, t => 450f + 2600f * flame(t));
                Dsp.HP(roar, 80f, rate);
                Dsp.Shape(roar, rate, flame);
                Dsp.Norm(roar);
                Dsp.Mix(x, roar, 0, 0.75f);
                float[] body = Dsp.Brown(g, n);
                Dsp.LP(body, 300f, rate);
                Dsp.Shape(body, rate, t => flame(t - 0.01f));
                Dsp.Norm(body);
                Dsp.Mix(x, body, 0, 0.45f);
                var cr = new float[n];
                Dsp.Grains(cr, g, 0.02f, 2.1f, 180, 1f, 0.0005f, u => MathF.Pow(1f - u, 1.6f));
                int pops = g.Int(5, 10);
                for (int k = 0; k < pops; k++)
                    Dsp.Mix(cr, Spark(g, g.Range(1400f, 3800f), g.Range(0.0006f, 0.0016f)), g.N(g.Range(0.03f, 1.6f)), g.Range(0.5f, 1.4f));
                Dsp.HP(cr, 1100f, rate);
                Dsp.Norm(cr);
                Dsp.Mix(x, cr, 0, 0.3f);
                Dsp.HP(x, 30f, rate);
                FadeTail(x, rate, 1.9f, 2.28f);
                return x;
            }

            // The staticbloom strikes: a few snaps a millisecond apart as the arc forms, the little thunder of the air it
            // heats, then a ragged sputtering buzz, crackle scattering through it and an ozone sizzle dying away.
            internal static float[] Electric(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(1.3f);
                int n = x.Length;
                const float T0 = 0.004f;
                int strikes = g.Int(2, 5);
                float at = T0;
                for (int k = 0; k < strikes; k++)
                {
                    Dsp.Mix(x, Spark(g, g.Range(2600f, 6500f), g.Range(0.0003f, 0.0007f)), (int)(at * rate), 0.6f - 0.08f * k);
                    at += g.Range(0.002f, 0.007f);
                }
                Dsp.Thump(x, rate, T0, 0.45f, 180f, 50f, 0.015f, 0.001f, 0.06f);
                Dsp.AddBurst(x, g, T0, 0.35f, 0.0003f, 0.012f, 0f, 900f);
                float arcLen = g.Range(0.3f, 0.6f), hz = g.Range(95f, 125f);
                var arc = new float[n];
                for (float tt = T0 + 0.01f; tt < T0 + arcLen; tt += g.Range(0.6f, 1.4f) / hz)
                {
                    int a0 = (int)(tt * rate);
                    float amp = g.Range(0.4f, 1.4f);
                    int len = g.N(g.Range(0.0015f, 0.005f));
                    for (int i = 0; i < len && a0 + i < n; i++) arc[a0 + i] += g.Bipolar() * amp * MathF.Exp(-i / (0.0009f * rate));
                }
                float[] spit = Dsp.Wander(g, n, rate, 30f), rag = Dsp.Wander(g, n, rate, 45f);
                double ph = 0;
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)rate;
                    float e = Dsp.ASR(t - T0, 0.01f, arcLen - 0.1f, 0.15f);
                    float f = hz * (0.92f + 0.16f * rag[i]);
                    arc[i] = (arc[i] + 0.4f * Dsp.Saw((float)ph, f / rate)) * e * (0.3f + 0.7f * Dsp.SmoothStep((spit[i] - 0.25f) / 0.2f));
                    ph += f / rate;
                    if (ph >= 1.0) ph -= 1.0;
                }
                Dsp.BP(arc, 1700f, 0.6f, rate);
                Dsp.LP(arc, 7000f, rate);
                Dsp.Norm(arc);
                Dsp.Mix(x, arc, 0, 0.8f);
                var cr = new float[n];
                Dsp.Grains(cr, g, T0, T0 + arcLen + 0.35f, 160, 1f, 0.0003f, u => 1f - 0.7f * u);
                Dsp.HP(cr, 2200f, rate);
                Dsp.Norm(cr);
                Dsp.Mix(x, cr, 0, 0.4f);
                float[] siz = Dsp.White(g, n);
                Dsp.HP(siz, 3500f, rate);
                Dsp.Shape(siz, rate, t => Dsp.AD(t - T0 - arcLen * 0.6f, 0.05f, 0.25f));
                Dsp.Norm(siz);
                Dsp.Mix(x, siz, 0, 0.08f);
                Dsp.HP(x, 40f, rate);
                Squash(x, 2.5f);
                Dsp.HP(x, 30f, rate);
                FadeTail(x, rate, 1.1f, 1.28f);
                return x;
            }

            // The staticbloom charging: a quickening run of crackles (each a little burst of sparks) over a whine climbing
            // underneath, a buzz on the last.
            internal static float[] Crackle(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(0.5f);
                int count = g.Int(6, 10);
                float last = 0f;
                for (int k = 0; k < count; k++)
                {
                    float u = k / (float)(count - 1);
                    float at = Math.Max(0.003f, 0.004f + 0.3f * MathF.Pow(u, 0.75f) + g.Range(-0.008f, 0.008f));
                    float gain = (0.45f + 0.55f * g.Next()) * (k == count - 1 ? 1.3f : 1f);
                    int sparks = g.Int(3, 7);
                    float span = g.Range(0.004f, 0.012f);
                    for (int s = 0; s < sparks; s++)
                        Dsp.Mix(x, Spark(g, g.Range(2200f, 6500f), g.Range(0.0003f, 0.0009f)), (int)((at + span * s / sparks) * rate), gain * g.Range(0.4f, 1f));
                    Dsp.AddBurst(x, g, at, gain * 0.3f, 0.0005f, span * 0.6f, 0f, 1500f);
                    last = Math.Max(last, at + span);
                }
                double ph = 0, bp = 0;
                for (int i = 0; i < x.Length; i++)
                {
                    float t = i / (float)rate;
                    float u = Dsp.Clamp01(t / last);
                    float w = t < last ? u * u : MathF.Exp(-(t - last) / 0.01f);
                    x[i] += 0.07f * w * Sin(ph);
                    ph += Dsp.Lerp(1800f, 3200f, u) / rate;
                    float b = Dsp.AD(t - last, 0.002f, 0.03f);
                    if (b > 0f) x[i] += 0.3f * b * Dsp.Saw((float)bp, 110f / rate);
                    bp += 110.0 / rate;
                    if (bp >= 1.0) bp -= 1.0;
                }
                Dsp.HP(x, 300f, rate);
                Squash(x, 2.5f);
                Dsp.HP(x, 200f, rate);
                return x;
            }

            // The grey syrup eating something: a dense frying sizzle that swells and fades, a breath of vapour, bubbles
            // small and quick and a few fat slow ones.
            internal static float[] Acid(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(2.1f);
                int n = x.Length;
                float[] w = Dsp.Wander(g, n, rate, 3f);
                float sus = g.Range(0.8f, 1.2f);
                var dens = new float[n];
                for (int i = 0; i < n; i++) dens[i] = Dsp.ASR(i / (float)rate, 0.05f, sus, 0.7f) * (0.5f + 0.5f * w[i]);
                var fry = new float[n];
                Fry(fry, g, 5000f, dens, 3f);
                Dsp.HP(fry, 2200f, rate);
                Dsp.LP(fry, 10000f, rate);
                Dsp.Norm(fry);
                Dsp.Mix(x, fry, 0, 0.5f);
                float[] hiss = Dsp.White(g, n);
                Dsp.BP(hiss, 5000f, 0.7f, rate);
                Dsp.Shape(hiss, rate, t => Dsp.ASR(t, 0.02f, 0.2f, 0.6f));
                Dsp.Norm(hiss);
                Dsp.Mix(x, hiss, 0, 0.15f);
                int bubbles = g.Int(10, 18);
                for (int k = 0; k < bubbles; k++)
                    Dsp.Mix(x, Dsp.Bubble(rate, g.Range(300f, 1300f), g.Range(0.015f, 0.06f), g.Range(0.6f, 2.2f)), g.N(g.Range(0.03f, 1.5f)), g.Range(0.1f, 0.35f));
                int fat = g.Int(2, 4);
                for (int k = 0; k < fat; k++)
                    Dsp.Mix(x, Dsp.Bubble(rate, g.Range(95f, 200f), g.Range(0.1f, 0.18f), g.Range(0.5f, 1f)), g.N(g.Range(0.1f, 1.3f)), g.Range(0.35f, 0.55f));
                Dsp.HP(x, 60f, rate);
                FadeTail(x, rate, 1.8f, 2.08f);
                return x;
            }

            // The tollgate takes its toll: a breath drawn in (noise and a cold shimmer swelling), then a crack, a deep
            // pressure boom and the air ringing like struck iron, a tone falling away as if the place bent back, and a few
            // dispersed reflections over a low tail.
            internal static float[] Trigger(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(2.8f);
                int n = x.Length;
                float hit = g.Range(0.12f, 0.16f);
                float[] swell = Dsp.Pink(g, n);
                Dsp.SweepBP(swell, rate, 2f, t => 300f * MathF.Pow(8f, Dsp.Clamp01(t / hit)));
                Dsp.Shape(swell, rate, t => t < hit ? (0.35f + 0.65f * MathF.Pow(t / hit, 2.5f)) * Math.Min(1f, t / 0.01f) : MathF.Exp(-(t - hit) / 0.02f));
                Dsp.Norm(swell);
                Dsp.Mix(x, swell, 0, 0.4f);
                Dsp.AddBurst(x, g, hit, 0.7f, 0.0003f, 0.005f, 0f, 700f);
                Dsp.Thump(x, rate, hit, 1f, g.Range(90f, 105f), 28f, 0.1f, 0.003f, 0.42f);
                Dsp.AddBurst(x, g, hit, 0.45f, 0.003f, 0.2f, 320f);
                float b = g.Range(150f, 190f);
                float[] iron = { 1f, 2.32f, 4.25f, 6.63f, 9.38f, 12.8f };
                var ring = new float[n];
                Dsp.Ring(ring, rate, hit, 1f, b, iron, 1.1f, 0.68f, 0.62f);
                Dsp.Ring(ring, rate, hit, 0.8f, b * 1.012f, iron, 0.9f, 0.68f, 0.62f);
                Dsp.Norm(ring);
                Dsp.Mix(x, ring, 0, 0.3f);
                double ph = 0;
                for (int i = (int)(hit * rate); i < n; i++)
                {
                    float t = i / (float)rate - hit;
                    float e = Dsp.AD(t, 0.01f, 0.35f);
                    if (t > 2f) break;
                    x[i] += 0.12f * e * Sin(ph);
                    ph += Dsp.Glide(1500f, 160f, t / 0.8f) / rate;
                }
                Reflect(x, g, 5, 0.07f, 0.35f, 0.25f, 2500f);
                float[] rum = Dsp.Brown(g, n);
                Dsp.LP(rum, 100f, rate);
                Dsp.Shape(rum, rate, t => Dsp.AD(t - hit, 0.05f, 0.7f));
                Dsp.Norm(rum);
                Dsp.Mix(x, rum, 0, 0.3f);
                Dsp.HP(x, 28f, rate);
                FadeTail(x, rate, 2.4f, 2.78f);
                return x;
            }
            #endregion

            #region Emission
            // A civil-defence siren on a pole somewhere across the Zone. The motor drives the rotor up for ~4½ s, it holds a
            // moment at full cry, then coasts down and is driven up again; a two-chamber rotor adds a second voice a minor
            // third up; the horn turns, sweeping past loud and bright twice a loop and dull facing away; the air rushing
            // through the ports rides the tone. Far off, the highs are gone, and the treeline, the blocks and the hills
            // answer with echoes from all round.
            internal static StereoBuf Siren(SynthGen g)
            {
                int rate = g.Rate;
                const float L = 12f, Lo = 175f, Hi = 470f, Rise = 0.37f, Hold = 0.1f;
                int n = g.N(L);
                float[] hz = Curve(n, rate, t =>
                {
                    float u = t / L, v;
                    if (u < Rise) v = (1f - MathF.Exp(-3.5f * u / Rise)) / (1f - MathF.Exp(-3.5f));
                    else if (u < Rise + Hold) v = 1f;
                    else
                    {
                        float s = (u - Rise - Hold) / (1f - Rise - Hold);
                        v = (MathF.Exp(-2.6f * s) - MathF.Exp(-2.6f)) / (1f - MathF.Exp(-2.6f));
                    }
                    return Lo + (Hi - Lo) * v;
                });
                float[] face = Curve(n, rate, t =>
                {
                    float c = 0.5f + 0.5f * MathF.Cos(Dsp.TwoPi * 2f * t / L + 0.9f);
                    return c * c;
                });
                float[] bright = Table(new[] { 1f, 0.42f, 0.5f, 0.2f, 0.28f, 0.1f, 0.15f, 0.06f, 0.08f, 0.035f, 0.045f, 0.02f });
                float[] dull = Table(new[] { 1f, 0.2f, 0.16f, 0.05f, 0.04f, 0.01f });
                double total = 0;
                for (int i = 0; i < n; i++) total += hz[i];
                total /= rate;
                double k1 = Math.Round(total) / total, k2 = Math.Round(total * 1.2) / (total * 1.2);
                var mono = new float[n];
                double p1 = 0, p2 = g.Next() * TabSize, s1k = k1 / rate * TabSize, s2k = 1.2 * k2 / rate * TabSize;
                for (int i = 0; i < n; i++)
                {
                    float b = face[i];
                    int j1 = (int)p1, j2 = (int)p2;
                    float f1 = (float)(p1 - j1), f2 = (float)(p2 - j2);
                    float d1 = dull[j1] + (dull[j1 + 1] - dull[j1]) * f1, b1 = bright[j1] + (bright[j1 + 1] - bright[j1]) * f1;
                    float d2 = dull[j2] + (dull[j2 + 1] - dull[j2]) * f2, b2 = bright[j2] + (bright[j2 + 1] - bright[j2]) * f2;
                    mono[i] = (0.32f + 0.68f * b) * (d1 + (b1 - d1) * b + 0.5f * (d2 + (b2 - d2) * b));
                    p1 += hz[i] * s1k;
                    if (p1 >= TabSize) p1 -= TabSize;
                    p2 += hz[i] * s2k;
                    if (p2 >= TabSize) p2 -= TabSize;
                }
                float[] air = Dsp.White(g, n);
                var centre = new float[n];
                for (int i = 0; i < n; i++) centre[i] = hz[i] * 2f;
                LoopSweep(air, rate, 2.5f, centre, false);
                Dsp.Norm(air, Dsp.Peak(mono) * 0.12f);
                for (int i = 0; i < n; i++) mono[i] += air[i] * (0.3f + 0.7f * face[i]);
                Dsp.LoopLP(mono, 2800f, rate);
                Dsp.LoopHP(mono, 90f, rate);
                var s = new StereoBuf(n);
                float pan = g.Range(-0.3f, 0.3f);
                Dsp.MixWrapPan(s, mono, 0, 0.75f, pan);
                var far = (float[])mono.Clone();
                Dsp.LoopLP(far, 1500f, rate);
                float[] delays = { 0.27f, 0.62f, 1.05f, 1.7f };
                float[] gains = { 0.4f, 0.3f, 0.2f, 0.13f };
                for (int e = 0; e < delays.Length; e++)
                    Dsp.MixWrapPan(s, far, g.N(delays[e] * g.Range(0.9f, 1.1f)), gains[e], Math.Max(-1f, Math.Min(1f, -pan * 1.6f + g.Range(-0.6f, 0.6f))));
                Space(s, rate, 2.4f, 2200f, 0.5f, 1.6f, true, 3.2f);
                return s;
            }

            // The earth under the emission: a jolt first (the ground's first shove, a deep thud and everything loose
            // rattling), then the roll swelling and dying — two sub-bass tones sliding against each other, saturated so
            // their harmonics carry on small speakers, under a deep rolling roar different in each ear — and the sky
            // cracking open here and there.
            internal static StereoBuf Rumble(SynthGen g)
            {
                int rate = g.Rate;
                float len = g.Range(4.1f, 4.8f);
                int n = g.N(len + 0.4f);
                var s = new StereoBuf(n);
                float peak = g.Range(0.8f, 1.5f);
                Func<float, float> env = t => t < peak ? 0.25f + 0.75f * MathF.Pow(Dsp.SmoothStep(t / peak), 1.4f) : MathF.Exp(-4f * (t - peak) / (len - peak));
                float a0 = g.Range(31f, 38f), a1 = a0 * g.Range(0.78f, 0.88f), b0 = g.Range(42f, 50f), b1 = b0 * g.Range(0.8f, 0.9f);
                float[] e = Curve(n, rate, env);
                for (int i = 0, f = g.N(0.15f); i < f; i++) e[i] *= i / (float)f;
                var sub = new float[n];
                Osc(sub, rate, s_Sin, Curve(n, rate, t => Dsp.Glide(a0, a1, t / len)), 0.6f, null, false);
                Osc(sub, rate, s_Sin, Curve(n, rate, t => Dsp.Glide(b0, b1, t / len)), 0.35f, null, false, 0.3);
                Grit(sub, 2.2f, 0.35f);
                Dsp.HP(sub, 24f, rate);
                Dsp.Norm(sub);
                var jolt = new float[n];
                const float J = 0.02f;
                Thud(jolt, rate, J, 1f, g.Range(52f, 60f), 30f, 0.08f, 0.004f, 0.22f);
                Dsp.AddBurst(jolt, g, J, 0.7f, 0.003f, 0.12f, 260f);
                var rattle = new float[n];
                Dsp.Grains(rattle, g, J, J + 0.5f, 90, 1f, 0.0008f, u => 1f - u);
                Dsp.HP(rattle, 900f, rate);
                Dsp.LP(rattle, 5000f, rate);
                Dsp.Norm(rattle);
                Dsp.Mix(jolt, rattle, 0, 0.12f);
                Dsp.Norm(jolt);
                for (int side = 0; side < 2; side++)
                {
                    float[] ch = side == 0 ? s.L : s.R;
                    float[] r = Dsp.Brown(g, n);
                    Dsp.LP(r, 85f, rate);
                    Dsp.Norm(r);
                    float[] m = Dsp.Brown(g, n);
                    Dsp.LP(m, 280f, rate);
                    Dsp.HP(m, 60f, rate);
                    Dsp.Norm(m);
                    float[] roll = Dsp.Wander(g, n, rate, 1.6f);
                    for (int i = 0; i < n; i++)
                        ch[i] += e[i] * (0.5f * sub[i] + 0.45f * r[i] + 0.5f * m[i] * (0.35f + 0.65f * roll[i])) + 0.55f * jolt[i];
                }
                int cracks = g.Int(2, 5);
                for (int c = 0; c < cracks; c++)
                {
                    float at = g.Range(0.35f, len * 0.7f);
                    Dsp.MixPan(s, SkyCrack(g, g.Range(0.15f, 0.45f)), g.N(at), g.Range(0.3f, 0.55f) * (0.4f + 0.6f * env(at)), g.Range(-0.9f, 0.9f));
                }
                Space(s, rate, 2.6f, 1800f, 0.35f, 1.2f, false);
                Dsp.HP(s.L, 22f, rate);
                Dsp.HP(s.R, 22f, rate);
                FadeTail(s.L, rate, len, len + 0.35f);
                FadeTail(s.R, rate, len, len + 0.35f);
                return s;
            }

            // A crack in the sky: a cluster of ragged bursts (the split and its echoes off the cloud), then a low roll.
            private static float[] SkyCrack(SynthGen g, float dur)
            {
                int rate = g.Rate;
                var c = new float[g.N(dur + 0.9f)];
                int bursts = g.Int(5, 12);
                for (int k = 0; k < bursts; k++)
                {
                    float at = dur * MathF.Pow(g.Next(), 1.6f);
                    Dsp.AddBurst(c, g, at, g.Range(0.3f, 1f) * (1f - 0.5f * at / dur), 0.0008f, g.Range(0.004f, 0.02f));
                }
                Dsp.HP(c, 200f, rate);
                Dsp.LP(c, 4200f, rate);
                Dsp.Norm(c);
                float[] roll = Dsp.Brown(g, c.Length);
                Dsp.LP(roll, 160f, rate);
                Dsp.Shape(roll, rate, t => Dsp.AD(t, 0.03f, 0.35f));
                Dsp.Norm(roll);
                Dsp.Mix(c, roll, 0, 0.6f);
                return c;
            }

            // The blowout itself: the front coming in (a roar rising and brightening, a howl climbing with it, sweeping across
            // from one side), the hit (a sub-bass boom, its crack and a low blast of air), then the wind behind it gusting
            // with debris and discharges cracking in the sky — all in a huge, wide space, the hit soft-limited so the
            // approach and the aftermath stand up under it.
            internal static StereoBuf Wave(SynthGen g)
            {
                int rate = g.Rate;
                const float Len = 6f, Hit = 1.6f;
                int n = g.N(Len), hitAt = g.N(Hit);
                var s = new StereoBuf(n);
                float[] gust = Curve(n, rate, Dsp.Gusts(g, Len, 2, 6, 3));
                for (int side = 0; side < 2; side++)
                {
                    float[] roar = Dsp.Pink(g, n);
                    Dsp.SweepLP(roar, rate, 0.9f, t => t < Hit ? 220f * MathF.Pow(2600f / 220f, MathF.Pow(t / Hit, 1.5f))
                        : 2600f * MathF.Pow(260f / 2600f, Dsp.Clamp01((t - Hit) / (Len - Hit))));
                    float early = side == 0 ? 1f : 0.55f, late = side == 0 ? 0.6f : 1f;
                    float[] env = Curve(n, rate, t => t < Hit ? (0.4f + 0.6f * MathF.Pow(t / Hit, 2.4f)) * Math.Min(1f, t / 0.05f) * Dsp.Lerp(early, late, t / Hit)
                        : MathF.Exp(-(t - Hit) / 1.9f) * late);
                    float[] ch = side == 0 ? s.L : s.R;
                    for (int i = 0; i < n; i++) ch[i] = roar[i] * env[i] * (i < hitAt ? 1f : 0.6f + 0.4f * (side == 0 ? gust[i] : 1f - gust[i]));
                }
                float k = 0.55f / Math.Max(Dsp.Peak(s.L), Dsp.Peak(s.R));
                Dsp.Gain(s.L, k);
                Dsp.Gain(s.R, k);
                // The mono layers (the space spreads them): the howl, the boom, the low blast and the air slamming past.
                float[] howl = Dsp.White(g, n);
                Dsp.SweepBP(howl, rate, 6f, t => t < Hit ? 260f + 520f * MathF.Pow(t / Hit, 1.3f) : 780f * MathF.Pow(0.3f, Dsp.Clamp01((t - Hit) / 2.5f)));
                float[] he = Curve(n, rate, t => t < Hit ? MathF.Pow(t / Hit, 3f) : MathF.Exp(-(t - Hit) / 0.9f));
                for (int i = 0; i < n; i++) howl[i] *= he[i];
                Dsp.Norm(howl, 0.2f);
                var boom = new float[n];
                Thud(boom, rate, Hit, 1f, 64f, 24f, 0.22f, 0.008f, 0.85f);
                Grit(boom, 2f, 0.3f, hitAt);
                Dsp.Gain(boom, 0.9f);
                float[] blast = Dsp.Brown(g, n);
                Dsp.LP(blast, 400f, rate);
                float[] air = Dsp.White(g, n);
                Dsp.BP(air, 1200f, 0.5f, rate);
                float[] be = Curve(n, rate, t => Dsp.AD(t - Hit, 0.01f, 1.1f));
                float[] ae = Curve(n, rate, t => Dsp.AD(t - Hit, 0.008f, 0.55f));
                float pb = 0f, pa = 0f;
                for (int i = hitAt; i < n; i++)
                {
                    blast[i] *= be[i];
                    air[i] *= ae[i];
                    pb = Math.Max(pb, Math.Abs(blast[i]));
                    pa = Math.Max(pa, Math.Abs(air[i]));
                }
                float kb = 0.5f / Math.Max(1e-9f, pb), ka = 0.6f / Math.Max(1e-9f, pa);
                for (int i = 0; i < n; i++)
                {
                    float m = howl[i] + boom[i] + (i >= hitAt ? blast[i] * kb + air[i] * ka : 0f);
                    s.L[i] += m;
                    s.R[i] += m;
                }
                float[] crack = Dsp.Burst(g, 0.0005f, 0.025f, 7000f, 300f);
                Dsp.Mix(s.L, crack, hitAt, 0.7f);
                Dsp.Mix(s.R, crack, hitAt + g.N(0.007f), 0.7f);
                // Debris pattering past, heard first on one side and its echo on the other.
                var debris = new float[n];
                Dsp.Grains(debris, g, Hit + 0.05f, Len - 1f, 220, 1f, 0.0007f, u => MathF.Pow(1f - u, 1.5f));
                Dsp.BP(debris, 2300f, 0.45f, rate);
                Dsp.Norm(debris, 0.12f);
                Dsp.Mix(s.L, debris, 0, 1f);
                Dsp.Mix(s.R, debris, g.N(0.11f), 0.8f);
                for (int c = 0; c < 2; c++)
                    Dsp.MixPan(s, SkyCrack(g, g.Range(0.2f, 0.5f)), g.N(Hit + g.Range(0.4f, 3.3f)), g.Range(0.3f, 0.45f), g.Range(-0.9f, 0.9f));
                Dsp.MixPan(s, SkyCrack(g, 0.3f), g.N(0.02f), 0.5f, -0.6f);
                Space(s, rate, 3f, 1600f, 0.45f, 1.4f, false);
                Squash(s, 2.4f);
                Dsp.HP(s.L, 22f, rate);
                Dsp.HP(s.R, 22f, rate);
                FadeTail(s.L, rate, Len - 1.2f, Len - 0.05f);
                FadeTail(s.R, rate, Len - 1.2f, Len - 0.05f);
                return s;
            }
            #endregion

            #region Helicopter and flare
            // A four-blade utility helicopter at full rotor speed (~255 rpm; the flight plays it slower on the ground): every
            // blade passage a low "whop" and the slap of the blade cutting its predecessor's vortex, the rotor wash chopped
            // by the blades, the tail rotor's buzz, the turboshaft's whine and the gearbox, the exhaust's roar.
            internal static float[] Rotor(SynthGen g)
            {
                int rate = g.Rate;
                const float L = 4f;
                const int Blades = 4;
                int n = g.N(L);
                var x = new float[n];
                float rev = Dsp.Snap(4.25f, L), bpf = rev * Blades;
                int passes = (int)MathF.Round(rev * L) * Blades;
                float[] track = { 1f, g.Range(0.68f, 0.76f), g.Range(0.9f, 0.97f), g.Range(0.62f, 0.7f) };
                var thump = new float[n];
                var chop = new float[n];
                var washEnv = new float[n];
                var ek = new float[g.N(0.08f)];
                for (int i = 0; i < ek.Length; i++) ek[i] = (1f - MathF.Exp(-i / (0.002f * rate))) * MathF.Exp(-i / (0.025f * rate));
                for (int k = 0; k < passes; k++)
                {
                    int at = (int)((k + 0.5f) / bpf * rate) + g.Int(-12, 13);
                    float a = track[k % Blades] * g.Range(0.9f, 1.08f);
                    var w = new float[g.N(0.12f)];
                    Dsp.Thump(w, rate, 0f, a, g.Range(78f, 88f), 62f, 0.02f, 0.0025f, 0.022f);
                    Dsp.MixWrap(thump, w, at, 1f);
                    Dsp.MixWrap(chop, Dsp.Burst(g, 0.0015f, g.Range(0.005f, 0.008f)), at, a);
                    Dsp.MixWrap(washEnv, ek, at, 1f);
                }
                Dsp.LoopBP(chop, 650f, 0.7f, rate);
                Dsp.LoopLP(chop, 2400f, rate);
                Dsp.Norm(chop);
                Dsp.Norm(thump);
                Dsp.Norm(washEnv);
                float[] wash = LoopPink(g, n);
                Dsp.LoopLP(wash, 1200f, rate);
                Dsp.Norm(wash);
                float[] tail = new float[n];
                var ta = new float[20];
                for (int h = 0; h < ta.Length; h++) ta[h] = 1f / MathF.Pow(h + 1, 1.2f);
                float[] tailTab = Table(ta);
                float tf = Dsp.Snap(bpf * 5.5f, L);
                var tfh = new float[n];
                for (int i = 0; i < n; i++) tfh[i] = tf;
                Osc(tail, rate, tailTab, tfh, 1f, null, true);
                Dsp.LoopLP(tail, 1500f, rate);
                Dsp.Norm(tail);
                float[] roar = LoopPink(g, n);
                Dsp.LoopBP(roar, 450f, 0.8f, rate);
                Dsp.Norm(roar);
                float[] drift = LoopWander(g, n, 0.7f);
                for (int i = 0; i < n; i++)
                    x[i] = (0.9f * thump[i] + 0.5f * chop[i] + 0.35f * wash[i] * (0.35f + 0.65f * washEnv[i]) + 0.16f * tail[i] + 0.12f * roar[i])
                        * (0.92f + 0.08f * drift[i]);
                Partial(x, rate, 6150f, 0.02f);
                Partial(x, rate, 6190f, 0.012f, null, 0.4);
                Partial(x, rate, 1210f, 0.025f);
                Dsp.LoopHP(x, 25f, rate);
                return x;
            }

            // A road flare burning: a fierce bright hiss ragged in level, spurts and pops of slag spitting off, and a low roar
            // felt more than heard.
            internal static float[] Flare(SynthGen g)
            {
                int rate = g.Rate;
                const float L = 4f;
                int n = g.N(L);
                var x = new float[n];
                float[] hiss = Dsp.White(g, n);
                Dsp.LoopHP(hiss, 900f, rate);
                Dsp.LoopPeakEq(hiss, 3500f, 0.8f, 5f, rate);
                Dsp.LoopLP(hiss, 11000f, rate);
                float[] fast = LoopWander(g, n, 35f), mid = LoopWander(g, n, 6f);
                for (int i = 0; i < n; i++) hiss[i] *= 0.55f + 0.25f * fast[i] + 0.2f * mid[i];
                Dsp.Norm(hiss);
                for (int i = 0; i < n; i++) x[i] = hiss[i] * 0.6f;
                var sp = new float[n];
                int spurts = (int)(L * g.Range(3f, 5f));
                for (int k = 0; k < spurts; k++)
                {
                    float dur = g.Range(0.02f, 0.08f);
                    float[] b = Dsp.White(g, g.N(dur));
                    Dsp.Shape(b, rate, t => Dsp.Bell(t, dur, 0.3f));
                    Dsp.MixWrap(sp, b, g.Int(0, n), g.Range(0.2f, 0.5f));
                }
                Dsp.LoopBP(sp, 2500f, 0.8f, rate);
                int pops = (int)(L * 6f);
                for (int k = 0; k < pops; k++) Dsp.MixWrap(sp, Spark(g, g.Range(1500f, 4000f), g.Range(0.0004f, 0.0012f)), g.Int(0, n), MathF.Pow(g.Next(), 1.8f) * 0.6f);
                Dsp.Norm(sp);
                for (int i = 0; i < n; i++) x[i] += sp[i] * 0.35f;
                float[] body = LoopBrown(g, n);
                Dsp.LoopLP(body, 280f, rate);
                Dsp.Norm(body);
                for (int i = 0; i < n; i++) x[i] += body[i] * 0.18f * (0.7f + 0.3f * mid[i]);
                Dsp.LoopHP(x, 40f, rate);
                return x;
            }
            #endregion

            #region The pickup
            // A cross-plane V8 fires in the order 1-8-4-3-6-5-7-2: left bank, right, right, left, right, left, left, right.
            // Each bank's pulses reach the single tailpipe through a different length of pipe, so the even train arrives
            // lumpy — the burble — and no two cylinders burn quite alike.
            private static readonly bool[] s_LeftBank = { true, false, false, true, false, true, true, false };

            // One engine's character: each firing slot's strength and pulse, and how late the left bank arrives.
            private sealed class V8
            {
                public readonly float[] Gain = new float[8];
                public readonly float[][] Pulse = new float[8][];
                public readonly float LeftLag;

                public V8(SynthGen g)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        Gain[c] = g.Range(0.7f, 1.2f);
                        Pulse[c] = ExhaustPulse(g, g.Range(0.96f, 1.04f));
                    }
                    LeftLag = g.Range(0.0014f, 0.0024f);
                }

                // Mixes the firing of slot c at t seconds, strength a, into x (wrapping round a loop if asked).
                public void Fire(float[] x, int rate, float t, int c, float a, bool wrap)
                {
                    c &= 7;
                    if (s_LeftBank[c]) t += LeftLag;
                    int at = (int)(t * rate + 0.5f);
                    if (wrap) Dsp.MixWrap(x, Pulse[c], at, a * Gain[c]);
                    else Dsp.Mix(x, Pulse[c], at, a * Gain[c]);
                }
            }

            // One firing as it leaves the tailpipe: a one-sided pressure kick ringing the pipes and the muffler (a handful of
            // low modes) with a spit of combustion noise on its front.
            private static float[] ExhaustPulse(SynthGen g, float scale)
            {
                int rate = g.Rate;
                var p = new float[(int)(0.05f * rate)];
                Dsp.Modes(p, rate, 0f, 1f,
                    new[] { 66f * scale, 138f * scale, 232f * scale, 405f * scale, 690f * scale, 1180f * scale },
                    new[] { 0.6f, g.Range(0.75f, 0.95f), g.Range(0.6f, 0.8f), g.Range(0.4f, 0.5f), g.Range(0.2f, 0.3f), g.Range(0.1f, 0.14f) },
                    new[] { 0.014f, 0.010f, 0.0065f, 0.0045f, 0.0028f, 0.0016f });
                float kt = 0.0009f / scale;
                for (int i = 0; i < p.Length; i++)
                {
                    float t = i / (float)rate;
                    p[i] += 1.3f * (t / kt) * MathF.Exp(1f - t / kt) + 0.25f * g.Bipolar() * MathF.Exp(-t / 0.0018f);
                }
                return p;
            }

            // The times (seconds) the cylinders fire between t0 and t1 for an rpm curve (a V8 fires four times a turn).
            private static List<float> Firings(Func<float, float> rpm, float t0, float t1, double phase0)
            {
                var list = new List<float>();
                const float Dt = 0.00025f;
                double ph = phase0;
                for (int s = 0; ; s++)
                {
                    float t = t0 + s * Dt;
                    if (t >= t1) break;
                    double next = ph + rpm(t) / 15.0 * Dt;
                    if (Math.Floor(next) > Math.Floor(ph)) list.Add(t + Dt * (float)((Math.Floor(next) - ph) / (next - ph)));
                    ph = next;
                }
                return list;
            }

            // The breath riding each pulse (hiss on the exhaust's envelope) and the valve train ticking, for a buffer of firings.
            private static void Mechanics(float[] ex, SynthGen g, float ticksPerSecond, float t0, float t1, bool wrap)
            {
                int rate = g.Rate, n = ex.Length;
                var env = new float[n];
                for (int i = 0; i < n; i++) env[i] = ex[i] * ex[i];
                if (wrap) LoopLP1(env, 40f, rate);
                else Dsp.LP1(env, 40f, rate);
                float[] breath = Dsp.White(g, n);
                for (int i = 0; i < n; i++) breath[i] *= MathF.Sqrt(Math.Max(0f, env[i]));
                if (wrap) Dsp.LoopBP(breath, 1300f, 0.6f, rate);
                else Dsp.BP(breath, 1300f, 0.6f, rate);
                float pk = Dsp.Peak(ex);
                Dsp.Norm(breath, pk * 0.22f);
                var tick = new float[n];
                int ticks = (int)((t1 - t0) * ticksPerSecond);
                for (int j = 0; j < ticks; j++)
                {
                    int at = (int)((t0 + (j + 0.25f) / ticksPerSecond) * rate);
                    float a = g.Next();
                    if (wrap) Dsp.MixWrap(tick, Spark(g, g.Range(2600f, 3800f), 0.0003f), at, a * a);
                    else Dsp.Mix(tick, Spark(g, g.Range(2600f, 3800f), 0.0003f), at, a * a);
                }
                Dsp.Norm(tick, pk * 0.05f);
                for (int i = 0; i < n; i++) ex[i] += breath[i] + tick[i];
            }

            // The truck idling, ~900 rpm at pitch 1 (it plays at 0.7 standing — a lumpy 630 — and up to 1.6–2 on the move):
            // the burbling firing train, hunting a percent or so round the loop, its breath and the lifters ticking.
            internal static float[] EngineIdle(SynthGen g)
            {
                int rate = g.Rate;
                const float L = 3f;
                int n = g.N(L);
                var x = new float[n];
                var v = new V8(g);
                float cycleHz = Dsp.Snap(7.5f, L);
                int fires = (int)MathF.Round(cycleHz * L) * 8;
                double fireRate = cycleHz * 8.0;
                float a1 = g.Range(0.008f, 0.014f), a2 = g.Range(0.003f, 0.007f);
                int c1 = g.Int(1, 3), c2 = g.Int(3, 7);
                float p1 = g.Next() * Dsp.TwoPi, p2 = g.Next() * Dsp.TwoPi;
                var times = new float[fires];
                double ph = 0;
                int placed = 0;
                for (int i = 0; i < n && placed < fires; i++)
                {
                    float t = i / (float)rate;
                    double f = fireRate * (1.0 + a1 * Math.Sin(Dsp.TwoPi * c1 * t / L + p1) + a2 * Math.Sin(Dsp.TwoPi * c2 * t / L + p2));
                    double next = ph + f / rate;
                    while (placed < fires && next >= placed + 0.5)
                    {
                        times[placed] = t + (float)((placed + 0.5 - ph) / (next - ph)) / rate;
                        placed++;
                    }
                    ph = next;
                }
                for (int j = 0; j < placed; j++) v.Fire(x, rate, times[j] + g.Range(-0.00012f, 0.00012f), j, 1f + 0.1f * g.Bipolar(), true);
                Mechanics(x, g, (float)fireRate * 2f, 0f, L, true);
                Dsp.LoopHP(x, 28f, rate);
                Dsp.LoopLP(x, 5500f, rate);
                return x;
            }

            // Turning the key: the solenoid clacks in, the starter drags the engine over through its compressions, a pop or
            // two and it catches, flares to ~1600 rpm and settles to a lumpy idle — fading as the idle loop takes over.
            internal static float[] EngineStart(SynthGen g)
            {
                int rate = g.Rate;
                const float Len = 2.8f;
                float[] x = g.Buffer(Len);
                var v = new V8(g);
                float crank = g.Range(0.55f, 0.85f), peakRpm = g.Range(1450f, 1750f), rise = g.Range(0.25f, 0.35f);
                const float Idle = 630f;
                Solenoid(x, g, 0.012f, 1f);
                Starter(x, g, 0.03f, crank, 0.5f, false);
                Func<float, float> rpm = t =>
                {
                    if (t < crank) return 190f;
                    float u = t - crank;
                    if (u < rise) return 190f + (peakRpm - 190f) * Dsp.SmoothStep(u / rise);
                    return Idle + (peakRpm - Idle) * MathF.Exp(-(u - rise) / 0.3f);
                };
                List<float> fire = Firings(rpm, crank - 0.09f, Len, 0.5);
                var ex = new float[x.Length];
                for (int j = 0; j < fire.Count; j++)
                {
                    float t = fire[j], u = t - crank;
                    if (u > 0f && u < 0.12f && g.Chance(0.25f)) continue;
                    float a = u < 0f ? 0.55f : u < 0.06f ? g.Range(0.5f, 0.9f) : u < rise + 0.15f ? 1.2f : 0.8f;
                    v.Fire(ex, rate, t + g.Range(-0.00015f, 0.00015f), j, a * (1f + 0.1f * g.Bipolar()), false);
                }
                Mechanics(ex, g, 80f, crank, Len, false);
                Dsp.HP(ex, 28f, rate);
                Dsp.LP(ex, 5500f, rate);
                Dsp.Mix(x, ex, 0, 1f);
                FadeTail(x, rate, 1.6f, Len - 0.05f);
                return x;
            }

            // It won't go: the solenoid, the starter grinding the engine over, a cough or two and nothing following, then the
            // key let go — the starter coasts down and the engine rocks back on its last compression.
            internal static float[] EngineStartFail(SynthGen g)
            {
                int rate = g.Rate;
                const float Len = 2.1f;
                float[] x = g.Buffer(Len);
                var v = new V8(g);
                float crank = g.Range(1.0f, 1.35f);
                Solenoid(x, g, 0.012f, 1f);
                Starter(x, g, 0.03f, crank, 0.55f, true);
                var ex = new float[x.Length];
                int coughs = g.Int(1, 3);
                for (int c = 0; c < coughs; c++)
                {
                    float t = g.Range(0.35f, crank - 0.1f);
                    v.Fire(ex, rate, t, g.Int(0, 8), g.Range(0.6f, 1f), false);
                    if (g.Chance(0.5f)) v.Fire(ex, rate, t + g.Range(0.07f, 0.1f), g.Int(0, 8), g.Range(0.3f, 0.5f), false);
                }
                Dsp.LP(ex, 5000f, rate);
                Dsp.Mix(x, ex, 0, 0.9f);
                Solenoid(x, g, crank + 0.02f, 0.35f);
                Dsp.Thump(x, rate, crank + 0.12f, 0.35f, 48f, 30f, 0.05f, 0.006f, 0.09f);
                Squash(x, 1.6f);
                Dsp.HP(x, 30f, rate);
                FadeTail(x, rate, Len - 0.3f, Len - 0.03f);
                return x;
            }

            // Switching off: a moment of idle, the key turns, one half-hearted last firing, the crank spinning down through
            // its compressions (a chuff from the pipes each, slowing), and the block rocking on its mounts as it stops.
            internal static float[] EngineStop(SynthGen g)
            {
                int rate = g.Rate;
                const float Len = 1.9f;
                float[] x = g.Buffer(Len);
                var v = new V8(g);
                float cut = g.Range(0.14f, 0.22f), stop = cut + g.Range(0.45f, 0.65f);
                Func<float, float> rpm = t => t < cut ? 630f : 630f * MathF.Pow(Math.Max(0f, 1f - (t - cut) / (stop - cut)), 0.7f) + 0.001f;
                List<float> fire = Firings(rpm, 0f, stop, 0.5);
                var ex = new float[x.Length];
                for (int j = 0; j < fire.Count; j++)
                {
                    float t = fire[j];
                    if (t < cut) v.Fire(ex, rate, t, j, 0.8f * (1f + 0.1f * g.Bipolar()), false);
                    else if (t < cut + 0.03f) v.Fire(ex, rate, t, j, 0.35f, false);
                    else
                    {
                        float u = (t - cut) / (stop - cut);
                        Dsp.AddBurst(ex, g, t, 0.35f * (1f - u), 0.004f, 0.02f, 450f);
                        Dsp.Thump(ex, rate, t, 0.25f * (1f - u), 70f, 50f, 0.02f, 0.003f, 0.025f);
                    }
                }
                Mechanics(ex, g, 84f, 0f, cut, false);
                Dsp.LP(ex, 5000f, rate);
                Dsp.Mix(x, ex, 0, 1f);
                Dsp.Thump(x, rate, stop, 0.55f, 34f, 22f, 0.08f, 0.012f, 0.16f);
                Dsp.AddBurst(x, g, stop, 0.25f, 0.006f, 0.05f, 300f);
                int rattles = g.Int(3, 6);
                for (int k = 0; k < rattles; k++)
                    Dsp.Mix(x, Spark(g, g.Range(900f, 2400f), g.Range(0.002f, 0.004f)), g.N(stop + g.Range(0.01f, 0.12f)), g.Range(0.08f, 0.15f));
                Squash(x, 1.5f);
                Dsp.HP(x, 25f, rate);
                FadeTail(x, rate, Len - 0.35f, Len - 0.03f);
                return x;
            }

            // The starter solenoid: a hard steel clack and the thunk of the plunger seating.
            private static void Solenoid(float[] x, SynthGen g, float at, float gain)
            {
                int rate = g.Rate;
                Dsp.Mix(x, Spark(g, g.Range(1700f, 2600f), g.Range(0.003f, 0.006f)), (int)(at * rate), 0.6f * gain);
                Dsp.Modes(x, rate, at, 0.35f * gain, new[] { g.Range(420f, 520f), g.Range(1100f, 1400f), g.Range(3000f, 3600f) },
                    new[] { 1f, 0.6f, 0.3f }, new[] { 0.012f, 0.007f, 0.003f });
                Dsp.Thump(x, rate, at, 0.3f * gain, 110f, 80f, 0.01f, 0.001f, 0.015f);
            }

            // The starter turning the engine over: the motor's whine (armature and pinion on the ring gear) sagging at every
            // compression, grinding, and each compression's chuff from the pipes. Released at t1 it coasts down (spinDown) or
            // is simply thrown out as the engine catches.
            private static void Starter(float[] x, SynthGen g, float t0, float t1, float gain, bool spinDown)
            {
                int rate = g.Rate;
                float coast = spinDown ? 0.3f : 0.05f;
                int i0 = (int)(t0 * rate), i1 = Math.Min(x.Length, (int)((t1 + coast * 4f) * rate));
                if (i1 <= i0) return;
                int len = i1 - i0;
                var y = new float[len];
                var grind = new float[len];
                var chuff = new float[len];
                float comp = g.Range(10.5f, 13f), whine = g.Range(420f, 520f);
                double cph = g.Next(), ph = 0;
                float prev = (float)cph;
                for (int i = 0; i < len; i++)
                {
                    float t = t0 + i / (float)rate;
                    float run = t < t1 ? Math.Min(1f, (t - t0) / 0.06f) : MathF.Exp(-(t - t1) / coast);
                    cph += comp * run / rate;
                    float c = (float)(cph - Math.Floor(cph));
                    if (c < prev && run > 0.1f)
                    {
                        float a = run * g.Range(0.7f, 1f);
                        Dsp.AddBurst(chuff, g, i / (float)rate, 0.5f * a, 0.004f, 0.022f, 500f);
                        Dsp.Thump(chuff, rate, i / (float)rate, 0.35f * a, 75f, 55f, 0.02f, 0.003f, 0.03f);
                    }
                    prev = c;
                    float s4 = MathF.Sin(Dsp.Pi * c);
                    s4 *= s4;
                    s4 *= s4;
                    float f = whine * run * (1f - 0.14f * s4) * (spinDown || t < t1 ? 1f : 1.2f);
                    ph += f / rate;
                    float p = (float)(ph - Math.Floor(ph));
                    y[i] = (0.6f * Dsp.Saw(p, Math.Max(1e-4f, f / rate)) + 0.3f * Sin(ph * 2.0)) * run * (0.85f + 0.15f * s4);
                    grind[i] = g.Bipolar() * run * (0.3f + 0.7f * s4);
                }
                Dsp.BP(grind, 1800f, 0.7f, rate);
                Dsp.Norm(grind);
                Dsp.HP(y, 150f, rate);
                Dsp.LP(y, 3000f, rate);
                Dsp.Norm(y);
                Dsp.Norm(chuff);
                for (int i = 0; i < len; i++) y[i] = 0.55f * y[i] + 0.2f * grind[i] + 0.6f * chuff[i];
                Dsp.Mix(x, y, i0, gain);
            }

            // An old pickup's door: the handle's lever and the latch letting go, a short hinge groan, then the slam — the door
            // and frame thudding (a few low modes), the cab's air pushed, the latch catching twice and the glass rattling.
            internal static float[] CarDoor(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(1.7f);
                Latch(x, g, 0.01f, 0.35f);
                float cr = g.Range(0.18f, 0.32f), creakHz = g.Range(35f, 70f);
                float[] creak = Dsp.Creak(g, cr, u => creakHz * (1f + 0.6f * u), u => Dsp.Bell(u, 1f, 0.35f),
                    new[] { g.Range(600f, 900f), g.Range(1300f, 1800f) }, new[] { 12f, 9f }, 0.15f);
                Dsp.Mix(x, creak, g.N(0.06f), 0.12f);
                float slam = g.Range(0.75f, 1.05f);
                Dsp.Modes(x, rate, slam, 0.9f,
                    new[] { g.Range(85f, 100f), g.Range(135f, 155f), g.Range(210f, 245f), g.Range(330f, 390f), g.Range(520f, 610f) },
                    new[] { 1f, 0.8f, 0.55f, 0.35f, 0.2f }, new[] { 0.06f, 0.045f, 0.035f, 0.025f, 0.015f });
                Dsp.AddBurst(x, g, slam, 0.7f, 0.0006f, 0.012f, 2200f);
                Dsp.Thump(x, rate, slam, 0.5f, 60f, 40f, 0.03f, 0.002f, 0.06f);
                Latch(x, g, slam + 0.004f, 0.6f);
                Latch(x, g, slam + g.Range(0.012f, 0.02f), 0.45f);
                Dsp.Modes(x, rate, slam + 0.003f, 0.12f, new[] { g.Range(1600f, 2200f), g.Range(2600f, 3800f) }, new[] { 1f, 0.7f }, new[] { 0.04f, 0.03f });
                var rattle = new float[x.Length];
                Dsp.Grains(rattle, g, slam + 0.01f, slam + 0.14f, 25, 1f, 0.0005f, u => 1f - u);
                Dsp.HP(rattle, 1500f, rate);
                Dsp.Norm(rattle);
                Dsp.Mix(x, rattle, 0, 0.08f);
                Dsp.HP(x, 40f, rate);
                FadeTail(x, rate, 1.45f, 1.68f);
                return x;
            }

            // A latch: a dry steel click with a short ring of the mechanism behind it.
            private static void Latch(float[] x, SynthGen g, float at, float gain)
            {
                int rate = g.Rate;
                Dsp.Mix(x, Spark(g, g.Range(1800f, 3200f), g.Range(0.002f, 0.004f)), (int)(at * rate), gain);
                Dsp.Modes(x, rate, at, gain * 0.4f, new[] { g.Range(900f, 1300f), g.Range(2400f, 3000f) }, new[] { 1f, 0.5f }, new[] { 0.008f, 0.004f });
            }

            // A dashboard toggle: the lever snapping over its spring — a hard plastic click with the dash's hollow knock
            // behind it and a tiny metal tick as the contact closes.
            internal static float[] CarSwitch(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(0.08f);
                const float T0 = 0.003f;
                Dsp.Mix(x, Spark(g, g.Range(2800f, 3600f), g.Range(0.0012f, 0.002f)), (int)(T0 * rate), 1f);
                Dsp.Modes(x, rate, T0, 0.3f, new[] { g.Range(1100f, 1400f), g.Range(4200f, 5200f) }, new[] { 1f, 0.6f }, new[] { 0.004f, 0.002f });
                Dsp.Mix(x, Spark(g, g.Range(5500f, 7000f), 0.0006f), (int)((T0 + g.Range(0.006f, 0.012f)) * rate), 0.35f);
                Dsp.HP(x, 400f, rate);
                return x;
            }

            // A tyre blowing out: the casing letting go (a hard crack, the pressure's thump, the wheel arch booming), the air
            // rushing out and dying away, then the shredded tread flapping against the arch, slowing with the wheel.
            internal static float[] TyreBurst(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(2.2f);
                int n = x.Length;
                const float T0 = 0.003f;
                Dsp.Mix(x, Dsp.Burst(g, 0.0002f, g.Range(0.002f, 0.004f), 0f, 400f), (int)(T0 * rate), 1f);
                Dsp.Thump(x, rate, T0, 0.9f, g.Range(130f, 160f), 55f, 0.03f, 0.001f, 0.06f);
                Dsp.AddBurst(x, g, T0, 0.6f, 0.0005f, 0.03f, 2500f);
                Dsp.Modes(x, rate, T0 + 0.001f, 0.35f, new[] { g.Range(160f, 200f), g.Range(260f, 320f), g.Range(420f, 520f) },
                    new[] { 1f, 0.7f, 0.4f }, new[] { 0.07f, 0.05f, 0.03f });
                float[] hiss = Dsp.White(g, n);
                Dsp.HP(hiss, 1200f, rate);
                Dsp.PeakEq(hiss, g.Range(3500f, 5000f), 1f, 6f, rate);
                Dsp.LP(hiss, 11000f, rate);
                float[] turb = Dsp.Wander(g, n, rate, 25f);
                float tauH = g.Range(0.25f, 0.4f);
                float[] he = Curve(n, rate, t => Dsp.AD(t - T0 - 0.004f, 0.006f, tauH));
                for (int i = 0; i < n; i++) hiss[i] *= he[i] * (0.6f + 0.4f * turb[i]);
                Dsp.Norm(hiss);
                Dsp.Mix(x, hiss, 0, 0.35f);
                // A loose flap of tread slapping the arch once a turn, the wheel slowing under it.
                float wheel = g.Range(7f, 10f), slowTo = g.Range(0.35f, 0.55f);
                const float F0 = 0.06f, F1 = 1.9f;
                for (float t = F0 + g.Range(0f, 0.05f); t < F1;)
                {
                    float u = (t - F0) / (F1 - F0), a = (1f - u) * (1f - u) * g.Range(0.6f, 1f);
                    Dsp.AddBurst(x, g, t, 0.35f * a, 0.0008f, g.Range(0.006f, 0.012f), 1800f, 150f);
                    Dsp.Thump(x, rate, t, 0.2f * a, 110f, 70f, 0.01f, 0.001f, 0.015f);
                    t += 1f / (wheel * Dsp.Lerp(1f, slowTo, u));
                }
                Dsp.HP(x, 35f, rate);
                Squash(x, 1.6f);
                Dsp.HP(x, 30f, rate);
                FadeTail(x, rate, 1.95f, 2.18f);
                return x;
            }
            #endregion

            #region Drones
            // An FPV quadcopter: four props a few hertz apart (the flight controller trims them constantly — beating, nervous),
            // each a buzzy blade-pass tone with a nasal hump from the frame, the motors' whine above and the prop wash
            // chopped by the blades.
            internal static float[] Quad(SynthGen g)
            {
                int rate = g.Rate;
                const float L = 3f;
                int n = g.N(L);
                var x = new float[n];
                float baseHz = g.Range(205f, 225f);
                float[] offs = { -3.1f, -0.9f, 1.4f, 3.6f };
                const int H = 28;
                for (int r = 0; r < 4; r++)
                {
                    var amps = new float[H];
                    var phases = new float[H];
                    for (int k = 1; k <= H; k++)
                    {
                        float f = baseHz * k, d1 = (f - 1400f) / 500f, d2 = (f - 3200f) / 700f;
                        float a = 1f / MathF.Pow(k, 0.85f) * (1f + 1.2f * MathF.Exp(-d1 * d1) + 0.6f * MathF.Exp(-d2 * d2));
                        amps[k - 1] = f > 7000f ? a * 0.3f : a;
                        phases[k - 1] = g.Next();
                    }
                    float[] tab = Table(amps, phases);
                    Dsp.Norm(tab);
                    float[] trim = LoopWander(g, n, g.Range(4f, 9f)), lvl = LoopWander(g, n, 2f);
                    var hz = new float[n];
                    var amp = new float[n];
                    for (int i = 0; i < n; i++)
                    {
                        hz[i] = (baseHz + offs[r]) * (1f + 0.012f * trim[i]);
                        amp[i] = 0.8f + 0.2f * lvl[i];
                    }
                    Osc(x, rate, tab, hz, 0.25f, amp, true, g.Next());
                }
                Partial(x, rate, 4600f, 0.03f);
                Partial(x, rate, 9200f, 0.01f);
                float[] wash = LoopPink(g, n);
                Dsp.LoopBP(wash, 900f, 0.6f, rate);
                Dsp.Norm(wash);
                double step = Math.Round(baseHz * (double)n / rate) / n, ph = 0;
                for (int i = 0; i < n; i++)
                {
                    x[i] += wash[i] * 0.15f * (0.6f + 0.4f * Read(s_Sin, ph));
                    ph += step;
                    if (ph >= 1.0) ph -= 1.0;
                }
                Dsp.LoopHP(x, 80f, rate);
                return x;
            }

            // A tracked ground drone: two geared electric motors whining a few hertz apart, the gearbox meshing (its whine
            // rippling at the shaft's turn), the track links clacking over the sprockets, and rubber rubbing.
            internal static float[] Rover(SynthGen g)
            {
                int rate = g.Rate;
                const float L = 3f;
                int n = g.N(L);
                var x = new float[n];
                float[] w1 = LoopWander(g, n, 3f), w2 = LoopWander(g, n, 3f);
                var a1 = new float[n];
                var a2 = new float[n];
                for (int i = 0; i < n; i++)
                {
                    a1[i] = 0.8f + 0.2f * w1[i];
                    a2[i] = 0.8f + 0.2f * w2[i];
                }
                float m = g.Range(300f, 330f);
                Partial(x, rate, m, 0.3f, a1);
                Partial(x, rate, m * 2f, 0.15f, a1, 0.3);
                Partial(x, rate, m * 3f, 0.08f, a1, 0.6);
                Partial(x, rate, m + 8f, 0.25f, a2, 0.2);
                Partial(x, rate, (m + 8f) * 2f, 0.12f, a2, 0.5);
                Partial(x, rate, 3900f, 0.02f);
                float[] shaft = Curve(n, rate, t => 0.6f + 0.4f * MathF.Sin(Dsp.TwoPi * 14f * t));
                Partial(x, rate, 1150f, 0.06f, shaft);
                var clack = new float[n];
                int links = (int)(L * 16f);
                for (int k = 0; k < links; k++)
                    Dsp.MixWrap(clack, Spark(g, g.Range(1800f, 2600f), 0.001f), (int)((k + 0.5f) / 16f * rate) + g.Int(-40, 41), g.Range(0.3f, 1f));
                Dsp.LoopLP(clack, 3000f, rate);
                Dsp.Norm(clack);
                for (int i = 0; i < n; i++) x[i] += clack[i] * 0.18f;
                float[] rub = LoopPink(g, n);
                Dsp.LoopLP(rub, 600f, rate);
                Dsp.Norm(rub);
                for (int i = 0; i < n; i++) x[i] += rub[i] * 0.1f;
                Dsp.LoopHP(x, 60f, rate);
                return x;
            }
            #endregion
        }
    }

    public static partial class SfxIds
    {
        /// <summary>Per-kind anomaly hums (the plain <see cref="AnomalyHum"/> is the psi kind's), the staticbloom's charge
        /// crackle, a revealed artifact's song, the emission's pressure wave, the truck's start, stop, door, dashboard switch
        /// and tyre blowout, and the ground drone's motors.</summary>
        public const string AnomalyHumGravity = "anomaly_hum_gravity", AnomalyHumFire = "anomaly_hum_fire",
            AnomalyHumElectric = "anomaly_hum_electric", AnomalyHumAcid = "anomaly_hum_acid", AnomalyCrackle = "anomaly_crackle",
            ArtifactHum = "artifact_hum", EmissionWave = "emission_wave",
            EngineStart = "engine_start", EngineStop = "engine_stop", CarDoor = "car_door", CarSwitch = "car_switch", TyreBurst = "tyre_burst",
            DroneRover = "drone_rover";
    }
}
