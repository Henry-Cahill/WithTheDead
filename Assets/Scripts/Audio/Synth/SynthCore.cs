using System;

namespace CloneZ
{
    /// <summary>
    /// The deterministic random source and buffer helpers for one generated sound (one variation of one id): the same
    /// id and variation always come out sample-for-sample identical. Pure C#, no engine types.
    /// </summary>
    internal sealed class SynthGen
    {
        public readonly int Rate;
        public readonly float Dt;
        public readonly int Variation;
        private uint _s;

        public SynthGen(int rate, uint seed, int variation)
        {
            Rate = rate;
            Dt = 1f / rate;
            Variation = variation;
            _s = seed != 0 ? seed : 0x6D2B79F5u;
            for (int i = 0; i < 8; i++) NextUInt();
        }

        public uint NextUInt()
        {
            uint x = _s;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _s = x;
            return x;
        }

        /// <summary>Uniform in [0, 1).</summary>
        public float Next() => (NextUInt() >> 8) * (1f / 16777216f);
        /// <summary>Uniform in [-1, 1).</summary>
        public float Bipolar() => Next() * 2f - 1f;
        public float Range(float a, float b) => a + (b - a) * Next();
        public int Int(int min, int maxExclusive) => maxExclusive <= min ? min : Math.Min(maxExclusive - 1, min + (int)(Next() * (maxExclusive - min)));
        public bool Chance(float p) => Next() < p;
        /// <summary>The value scaled by a random factor in [1 - spread, 1 + spread].</summary>
        public float Vary(float v, float spread) => v * (1f + Bipolar() * spread);
        public int N(float seconds) => Math.Max(1, (int)(seconds * Rate + 0.5f));
        public float[] Buffer(float seconds) => new float[N(seconds)];
    }

    /// <summary>A one-pole filter (6 dB/octave) — the cheapest smoothing there is.</summary>
    internal struct OnePole
    {
        public float A, Z;

        public static OnePole At(float hz, int rate)
        {
            var f = new OnePole();
            f.Set(hz, rate);
            return f;
        }

        public void Set(float hz, int rate) => A = MathF.Exp(-Dsp.TwoPi * Math.Min(Math.Max(hz, 0.01f), rate * 0.45f) / rate);
        public float Lp(float x) { Z = x + (Z - x) * A; return Z; }
        public float Hp(float x) => x - Lp(x);

        /// <summary>The factor that brings this lowpass's output, fed uniform white noise in [-1, 1), to unit deviation.</summary>
        public float NoiseNormaliser => 1f / MathF.Sqrt(Math.Max(1e-12f, (1f / 3f) * (1f - A) / (1f + A)));
    }

    /// <summary>An RBJ-cookbook biquad (transposed direct form II): low/high/band-pass and peaking.</summary>
    internal struct Biquad
    {
        private float _b0, _b1, _b2, _a1, _a2, _z1, _z2;

        public static Biquad LP(float hz, float q, int rate) { var b = new Biquad(); b.SetLP(hz, q, rate); return b; }
        public static Biquad HP(float hz, float q, int rate) { var b = new Biquad(); b.SetHP(hz, q, rate); return b; }
        public static Biquad BP(float hz, float q, int rate) { var b = new Biquad(); b.SetBP(hz, q, rate); return b; }
        public static Biquad Peak(float hz, float q, float db, int rate) { var b = new Biquad(); b.SetPeak(hz, q, db, rate); return b; }

        public void SetLP(float hz, float q, int rate)
        {
            Prep(hz, q, rate, out float cs, out float alpha);
            float k = 1f - cs;
            Set(k * 0.5f, k, k * 0.5f, 1f + alpha, -2f * cs, 1f - alpha);
        }

        public void SetHP(float hz, float q, int rate)
        {
            Prep(hz, q, rate, out float cs, out float alpha);
            float k = 1f + cs;
            Set(k * 0.5f, -k, k * 0.5f, 1f + alpha, -2f * cs, 1f - alpha);
        }

        /// <summary>Band-pass with a 0 dB peak.</summary>
        public void SetBP(float hz, float q, int rate)
        {
            Prep(hz, q, rate, out float cs, out float alpha);
            Set(alpha, 0f, -alpha, 1f + alpha, -2f * cs, 1f - alpha);
        }

        public void SetPeak(float hz, float q, float db, int rate)
        {
            Prep(hz, q, rate, out float cs, out float alpha);
            float a = MathF.Pow(10f, db / 40f);
            Set(1f + alpha * a, -2f * cs, 1f - alpha * a, 1f + alpha / a, -2f * cs, 1f - alpha / a);
        }

        private static void Prep(float hz, float q, int rate, out float cs, out float alpha)
        {
            float f = Math.Max(8f, Math.Min(hz, rate * 0.45f));
            float w = Dsp.TwoPi * f / rate;
            cs = MathF.Cos(w);
            alpha = MathF.Sin(w) / (2f * Math.Max(0.05f, q));
        }

        private void Set(float b0, float b1, float b2, float a0, float a1, float a2)
        {
            float inv = 1f / a0;
            _b0 = b0 * inv;
            _b1 = b1 * inv;
            _b2 = b2 * inv;
            _a1 = a1 * inv;
            _a2 = a2 * inv;
        }

        public float Run(float x)
        {
            float y = _b0 * x + _z1;
            _z1 = _b1 * x - _a1 * y + _z2;
            _z2 = _b2 * x - _a2 * y;
            return y;
        }
    }

    /// <summary>Three formant frequencies and bandwidths (Hz) of a vowel.</summary>
    internal struct Vowel
    {
        public float F1, F2, F3, B1, B2, B3;

        public Vowel(float f1, float f2, float f3, float b1 = 90f, float b2 = 120f, float b3 = 170f)
        {
            F1 = f1; F2 = f2; F3 = f3; B1 = b1; B2 = b2; B3 = b3;
        }

        public static readonly Vowel A = new Vowel(730f, 1090f, 2440f);
        public static readonly Vowel Aw = new Vowel(570f, 840f, 2410f);
        public static readonly Vowel U = new Vowel(300f, 870f, 2240f, 70f, 110f, 160f);
        public static readonly Vowel Uh = new Vowel(640f, 1190f, 2390f);
        public static readonly Vowel E = new Vowel(530f, 1840f, 2480f);
        public static readonly Vowel Ae = new Vowel(660f, 1720f, 2410f);
        public static readonly Vowel I = new Vowel(270f, 2290f, 3010f, 60f, 120f, 180f);
        public static readonly Vowel O = new Vowel(450f, 800f, 2400f);
        public static readonly Vowel Er = new Vowel(490f, 1350f, 1690f);
        public static readonly Vowel[] Speech = { A, Aw, U, Uh, E, Ae, I, O, Er };

        public static Vowel Lerp(Vowel a, Vowel b, float t)
        {
            t = Dsp.Clamp01(t);
            return new Vowel(a.F1 + (b.F1 - a.F1) * t, a.F2 + (b.F2 - a.F2) * t, a.F3 + (b.F3 - a.F3) * t,
                a.B1 + (b.B1 - a.B1) * t, a.B2 + (b.B2 - a.B2) * t, a.B3 + (b.B3 - a.B3) * t);
        }

        /// <summary>Formants scaled by <paramref name="k"/> (a larger throat &lt; 1), bandwidths widened by <paramref name="widen"/>.</summary>
        public Vowel Scale(float k, float widen = 1f) => new Vowel(F1 * k, F2 * k, F3 * k, B1 * widen, B2 * widen, B3 * widen);
    }

    /// <summary>Settings for <see cref="Dsp.Voice"/>: a glottal source through three formants. Curves take u = 0..1 over the sound.</summary>
    internal sealed class VoiceParams
    {
        public Func<float, float> F0 = u => 110f;
        public Func<float, Vowel> Formants = u => Vowel.Uh;
        public Func<float, float> Amp = u => 1f;
        /// <summary>Aspiration noise mixed with the voiced source (0 = clean voice).</summary>
        public float Breath = 0.15f;
        /// <summary>Period-doubling growl and amplitude flutter (0..1).</summary>
        public float Rough;
        /// <summary>Wet, gargling amplitude bursts (0..1).</summary>
        public float Gurgle;
        public float VibratoHz = 5f, VibratoDepth;
        /// <summary>Slow random pitch wander (standard deviation, as a fraction of f0).</summary>
        public float Jitter = 0.01f;
        /// <summary>Lowpass on the glottal source (spectral tilt).</summary>
        public float Tilt = 2200f;
        public float G1 = 1f, G2 = 0.6f, G3 = 0.3f;
        /// <summary>Unformanted source mixed in (body).</summary>
        public float Direct = 0.05f;
        /// <summary>Noise only: a whisper.</summary>
        public bool Whisper;
    }

    /// <summary>
    /// The synthesiser's toolbox: noise, oscillators, filters, envelopes, modal resonances, formant voices, echoes,
    /// a small reverb and the loop tools (cross-faded seams, wrapped events, zero-crossing starts). Everything works
    /// on plain float buffers at the buffer's own sample rate.
    /// </summary>
    internal static class Dsp
    {
        public const float Pi = 3.14159265f, TwoPi = 6.28318531f;

        #region Maths
        public static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
        public static float Lerp(float a, float b, float t) => a + (b - a) * t;
        public static float SmoothStep(float t) { t = Clamp01(t); return t * t * (3f - 2f * t); }
        /// <summary>Exponential glide from a to b as t goes 0..1 (a and b &gt; 0).</summary>
        public static float Glide(float a, float b, float t) => a * MathF.Pow(b / a, Clamp01(t));
        public static float Midi(float note) => 440f * MathF.Pow(2f, (note - 69f) / 12f);
        /// <summary>A frequency nudged to a whole number of cycles per loop, so a tone wraps seamlessly.</summary>
        public static float Snap(float hz, float loopSeconds) => MathF.Max(1f, MathF.Round(hz * loopSeconds)) / loopSeconds;
        #endregion

        #region Envelopes
        /// <summary>Linear attack then exponential decay (tau seconds).</summary>
        public static float AD(float t, float attack, float tau)
        {
            if (t < 0f) return 0f;
            if (t < attack) return t / attack;
            return MathF.Exp(-(t - attack) / tau);
        }

        /// <summary>sin² hump over [0, len], peaking at peak (0..1 of len).</summary>
        public static float Bell(float t, float len, float peak = 0.5f)
        {
            if (t <= 0f || t >= len) return 0f;
            float p = len * Math.Min(0.98f, Math.Max(0.02f, peak));
            float s = t < p ? MathF.Sin(0.5f * Pi * t / p) : MathF.Cos(0.5f * Pi * (t - p) / (len - p));
            return s * s;
        }

        /// <summary>Smooth attack, sustain, smooth release.</summary>
        public static float ASR(float t, float attack, float sustain, float release)
        {
            if (t <= 0f) return 0f;
            if (t < attack) return SmoothStep(t / attack);
            t -= attack;
            if (t < sustain) return 1f;
            t -= sustain;
            return t < release ? 1f - SmoothStep(t / release) : 0f;
        }

        /// <summary>A periodic, smooth 0..1 curve (a few slow sines with whole cycles per period), seeded.</summary>
        public static Func<float, float> Gusts(SynthGen g, float period, int minCycles, int maxCycles, int terms)
        {
            var cyc = new float[terms];
            var ph = new float[terms];
            var amp = new float[terms];
            float sum = 0f;
            for (int i = 0; i < terms; i++)
            {
                cyc[i] = g.Int(minCycles, maxCycles + 1);
                ph[i] = g.Next() * TwoPi;
                amp[i] = 1f / (1f + i * 0.6f);
                sum += amp[i];
            }
            return t =>
            {
                float v = 0f;
                for (int i = 0; i < cyc.Length; i++) v += amp[i] * MathF.Sin(TwoPi * cyc[i] * t / period + ph[i]);
                return 0.5f + 0.5f * v / sum;
            };
        }
        #endregion

        #region Oscillators
        private static float Blep(float t, float dt)
        {
            if (t < dt)
            {
                t /= dt;
                return t + t - t * t - 1f;
            }
            if (t > 1f - dt)
            {
                t = (t - 1f) / dt;
                return t * t + t + t + 1f;
            }
            return 0f;
        }

        /// <summary>Band-limited sawtooth at phase 0..1 advancing dt per sample.</summary>
        public static float Saw(float phase, float dt) => 2f * phase - 1f - Blep(phase, Math.Min(0.5f, dt));

        /// <summary>Band-limited square.</summary>
        public static float Square(float phase, float dt)
        {
            dt = Math.Min(0.5f, dt);
            float v = phase < 0.5f ? 1f : -1f;
            v += Blep(phase, dt);
            float p2 = phase + 0.5f;
            if (p2 >= 1f) p2 -= 1f;
            return v - Blep(p2, dt);
        }

        public static float Wrap(float p) => p - MathF.Floor(p);
        #endregion

        #region Noise
        public static float[] White(SynthGen g, int n, float amp = 1f)
        {
            var x = new float[n];
            for (int i = 0; i < n; i++) x[i] = g.Bipolar() * amp;
            return x;
        }

        /// <summary>Pink (-3 dB/octave) noise, roughly unit peak.</summary>
        public static float[] Pink(SynthGen g, int n)
        {
            var x = new float[n];
            float b0 = 0f, b1 = 0f, b2 = 0f;
            for (int i = 0; i < n; i++)
            {
                float w = g.Bipolar();
                b0 = 0.99765f * b0 + w * 0.0990460f;
                b1 = 0.96300f * b1 + w * 0.2965164f;
                b2 = 0.57000f * b2 + w * 1.0526913f;
                x[i] = (b0 + b1 + b2 + w * 0.1848f) * 0.22f;
            }
            return x;
        }

        /// <summary>Brown (-6 dB/octave) noise from a leaky integrator, normalised to unit peak.</summary>
        public static float[] Brown(SynthGen g, int n)
        {
            var x = new float[n];
            float z = 0f;
            for (int i = 0; i < n; i++)
            {
                z = z * 0.985f + g.Bipolar() * 0.12f;
                x[i] = z;
            }
            Norm(x);
            return x;
        }

        /// <summary>Smooth random wander 0..1 (noise lowpassed twice at <paramref name="hz"/>), normalised.</summary>
        public static float[] Wander(SynthGen g, int n, int rate, float hz)
        {
            var x = White(g, n);
            LP1(x, hz, rate);
            LP1(x, hz, rate);
            float lo = float.MaxValue, hi = float.MinValue;
            for (int i = 0; i < n; i++) { if (x[i] < lo) lo = x[i]; if (x[i] > hi) hi = x[i]; }
            float span = Math.Max(1e-9f, hi - lo);
            for (int i = 0; i < n; i++) x[i] = (x[i] - lo) / span;
            return x;
        }
        #endregion

        #region Filters
        public static void LP(float[] x, float hz, int rate, float q = 0.7071f)
        {
            Biquad f = Biquad.LP(hz, q, rate);
            for (int i = 0; i < x.Length; i++) x[i] = f.Run(x[i]);
        }

        public static void HP(float[] x, float hz, int rate, float q = 0.7071f)
        {
            Biquad f = Biquad.HP(hz, q, rate);
            for (int i = 0; i < x.Length; i++) x[i] = f.Run(x[i]);
        }

        public static void BP(float[] x, float hz, float q, int rate)
        {
            Biquad f = Biquad.BP(hz, q, rate);
            for (int i = 0; i < x.Length; i++) x[i] = f.Run(x[i]);
        }

        public static void PeakEq(float[] x, float hz, float q, float db, int rate)
        {
            Biquad f = Biquad.Peak(hz, q, db, rate);
            for (int i = 0; i < x.Length; i++) x[i] = f.Run(x[i]);
        }

        public static void LP1(float[] x, float hz, int rate)
        {
            OnePole f = OnePole.At(hz, rate);
            for (int i = 0; i < x.Length; i++) x[i] = f.Lp(x[i]);
        }

        public static void HP1(float[] x, float hz, int rate)
        {
            OnePole f = OnePole.At(hz, rate);
            for (int i = 0; i < x.Length; i++) x[i] = f.Hp(x[i]);
        }

        /// <summary>A lowpass whose cutoff follows hz(t seconds), refreshed every 16 samples.</summary>
        public static void SweepLP(float[] x, int rate, float q, Func<float, float> hz)
        {
            var f = new Biquad();
            for (int i = 0; i < x.Length; i++)
            {
                if ((i & 15) == 0) f.SetLP(hz(i / (float)rate), q, rate);
                x[i] = f.Run(x[i]);
            }
        }

        /// <summary>A band-pass whose centre follows hz(t seconds), refreshed every 16 samples.</summary>
        public static void SweepBP(float[] x, int rate, float q, Func<float, float> hz)
        {
            var f = new Biquad();
            for (int i = 0; i < x.Length; i++)
            {
                if ((i & 15) == 0) f.SetBP(hz(i / (float)rate), q, rate);
                x[i] = f.Run(x[i]);
            }
        }

        /// <summary>Runs a lowpass over a loop twice so the filter's state wraps around the seam.</summary>
        public static void LoopLP(float[] x, float hz, int rate, float q = 0.7071f)
        {
            Biquad f = Biquad.LP(hz, q, rate);
            for (int i = 0; i < x.Length; i++) f.Run(x[i]);
            for (int i = 0; i < x.Length; i++) x[i] = f.Run(x[i]);
        }

        public static void LoopHP(float[] x, float hz, int rate, float q = 0.7071f)
        {
            Biquad f = Biquad.HP(hz, q, rate);
            for (int i = 0; i < x.Length; i++) f.Run(x[i]);
            for (int i = 0; i < x.Length; i++) x[i] = f.Run(x[i]);
        }

        public static void LoopBP(float[] x, float hz, float q, int rate)
        {
            Biquad f = Biquad.BP(hz, q, rate);
            for (int i = 0; i < x.Length; i++) f.Run(x[i]);
            for (int i = 0; i < x.Length; i++) x[i] = f.Run(x[i]);
        }

        public static void LoopPeakEq(float[] x, float hz, float q, float db, int rate)
        {
            Biquad f = Biquad.Peak(hz, q, db, rate);
            for (int i = 0; i < x.Length; i++) f.Run(x[i]);
            for (int i = 0; i < x.Length; i++) x[i] = f.Run(x[i]);
        }
        #endregion

        #region Building blocks
        public static void Shape(float[] x, int rate, Func<float, float> env)
        {
            for (int i = 0; i < x.Length; i++) x[i] *= env(i / (float)rate);
        }

        /// <summary>Adds src into dst starting at sample <paramref name="at"/> (clipped to dst).</summary>
        public static void Mix(float[] dst, float[] src, int at, float gain = 1f)
        {
            int i0 = Math.Max(0, -at);
            int i1 = Math.Min(src.Length, dst.Length - at);
            for (int i = i0; i < i1; i++) dst[at + i] += src[i] * gain;
        }

        /// <summary>Adds src into a loop starting at <paramref name="at"/>, wrapping round its end.</summary>
        public static void MixWrap(float[] loop, float[] src, int at, float gain = 1f)
        {
            int n = loop.Length;
            int k = ((at % n) + n) % n;
            for (int i = 0; i < src.Length; i++)
            {
                loop[k] += src[i] * gain;
                if (++k >= n) k = 0;
            }
        }

        /// <summary>White noise with an attack/exponential-decay envelope, optionally high- and/or low-passed.</summary>
        public static float[] Burst(SynthGen g, float attack, float tau, float lpHz = 0f, float hpHz = 0f)
        {
            int rate = g.Rate;
            int n = (int)((attack + tau * 7f) * rate) + 2;
            var b = new float[n];
            for (int i = 0; i < n; i++) b[i] = g.Bipolar() * AD(i / (float)rate, attack, tau);
            if (hpHz > 0f) HP(b, hpHz, rate);
            if (lpHz > 0f) LP(b, lpHz, rate);
            return b;
        }

        public static void AddBurst(float[] x, SynthGen g, float at, float gain, float attack, float tau, float lpHz = 0f, float hpHz = 0f)
            => Mix(x, Burst(g, attack, tau, lpHz, hpHz), (int)(at * g.Rate), gain);

        /// <summary>A band-passed noise burst (normalised before the gain).</summary>
        public static void AddBurstBP(float[] x, SynthGen g, float at, float gain, float attack, float tau, float hz, float q)
        {
            float[] b = Burst(g, attack, tau);
            BP(b, hz, q, g.Rate);
            Norm(b);
            Mix(x, b, (int)(at * g.Rate), gain);
        }

        /// <summary>A sine whose pitch glides from hz0 toward hz1 (time constant glideTau) under an AD envelope.</summary>
        public static void Thump(float[] x, int rate, float at, float gain, float hz0, float hz1, float glideTau, float attack, float tau)
        {
            int start = (int)(at * rate);
            int len = (int)((attack + tau * 7f) * rate);
            float ph = 0f, dt = 1f / rate;
            for (int i = 0; i < len; i++)
            {
                int k = start + i;
                if (k >= x.Length) break;
                float t = i * dt;
                float f = hz1 + (hz0 - hz1) * MathF.Exp(-t / glideTau);
                ph += f * dt;
                if (ph >= 1f) ph -= 1f;
                if (k >= 0) x[k] += gain * AD(t, attack, tau) * MathF.Sin(TwoPi * ph);
            }
        }

        /// <summary>
        /// Adds exponentially decaying sinusoids (an object's resonant modes) from <paramref name="at"/> seconds: the
        /// ring of struck metal, glass or wood. Each mode starts at zero phase, so the strike itself never clicks.
        /// </summary>
        public static void Modes(float[] x, int rate, float at, float gain, float[] hz, float[] amp, float[] tau)
        {
            int start = (int)(at * rate);
            for (int m = 0; m < hz.Length; m++)
            {
                float f = hz[m];
                if (f <= 0f || f >= rate * 0.47f) continue;
                float w = TwoPi * f / rate;
                float c = MathF.Cos(w), s = MathF.Sin(w);
                float tm = Math.Max(1e-4f, tau[Math.Min(m, tau.Length - 1)]);
                float d = MathF.Exp(-1f / (tm * rate));
                float re = 1f, im = 0f;
                float a = gain * amp[Math.Min(m, amp.Length - 1)];
                int end = Math.Min(x.Length, start + (int)(tm * rate * 7f) + 1);
                for (int k = start; k < end; k++)
                {
                    if (k >= 0) x[k] += a * im;
                    float nr = (re * c - im * s) * d;
                    im = (re * s + im * c) * d;
                    re = nr;
                }
            }
        }

        /// <summary>Modes at a base frequency times inharmonic ratios, higher partials quieter and dying faster.</summary>
        public static void Ring(float[] x, int rate, float at, float gain, float baseHz, float[] ratios, float tau0, float tauFall = 0.75f, float ampFall = 0.7f)
        {
            int n = ratios.Length;
            var hz = new float[n];
            var amp = new float[n];
            var tau = new float[n];
            float a = 1f, t = tau0;
            for (int i = 0; i < n; i++)
            {
                hz[i] = baseHz * ratios[i];
                amp[i] = a;
                tau[i] = t;
                a *= ampFall;
                t *= tauFall;
            }
            Modes(x, rate, at, gain, hz, amp, tau);
        }

        /// <summary>
        /// Scatters tiny decaying noise grains between t0 and t1 (seconds), their density and level following
        /// envelope(u) over that span — the crunch of snow, the crackle of fire, the tear of cloth.
        /// </summary>
        public static void Grains(float[] x, SynthGen g, float t0, float t1, int count, float gain, float grainTau, Func<float, float> envelope)
        {
            int rate = g.Rate;
            int glen = Math.Max(2, (int)(grainTau * rate * 6f));
            for (int c = 0; c < count; c++)
            {
                float u = 0f, e = 0f;
                for (int tries = 0; tries < 8; tries++)
                {
                    u = g.Next();
                    e = envelope != null ? Clamp01(envelope(u)) : 1f;
                    if (g.Next() <= e) break;
                }
                int at = (int)((t0 + (t1 - t0) * u) * rate);
                float amp = gain * (0.25f + 0.75f * g.Next()) * (0.3f + 0.7f * e);
                float tg = grainTau * (0.5f + g.Next());
                for (int i = 0; i < glen; i++)
                {
                    int k = at + i;
                    if (k < 0) continue;
                    if (k >= x.Length) break;
                    x[k] += g.Bipolar() * amp * MathF.Exp(-i / (tg * rate));
                }
            }
        }

        /// <summary>Like <see cref="Grains"/> but spread evenly round a loop (wrapping its end), levels skewed quiet.</summary>
        public static void GrainsWrap(float[] loop, SynthGen g, int count, float gain, float grainTau)
        {
            int rate = g.Rate;
            int n = loop.Length;
            int glen = Math.Max(2, (int)(grainTau * rate * 6f));
            for (int c = 0; c < count; c++)
            {
                int at = g.Int(0, n);
                float amp = gain * MathF.Pow(g.Next(), 2.2f);
                float tg = grainTau * (0.5f + g.Next());
                for (int i = 0; i < glen; i++) loop[(at + i) % n] += g.Bipolar() * amp * MathF.Exp(-i / (tg * rate));
            }
        }

        /// <summary>
        /// A creak: a stick-slip pulse train whose rate follows rateHz(u) exciting narrow resonances (a hinge, a
        /// floorboard, a tree). Normalised to unit peak; level shaped by amp(u).
        /// </summary>
        public static float[] Creak(SynthGen g, float seconds, Func<float, float> rateHz, Func<float, float> amp, float[] resHz, float[] resQ, float jitter)
        {
            int rate = g.Rate;
            int n = Math.Max(1, (int)(seconds * rate));
            var src = new float[n];
            float ph = 0f;
            for (int i = 0; i < n; i++)
            {
                float u = i / (float)n;
                ph += rateHz(u) * (1f + g.Bipolar() * jitter) / rate;
                if (ph >= 1f)
                {
                    ph -= 1f;
                    src[i] += 0.55f + 0.45f * g.Next();
                }
            }
            var y = new float[n];
            for (int r = 0; r < resHz.Length; r++)
            {
                Biquad f = Biquad.BP(resHz[r], resQ[Math.Min(r, resQ.Length - 1)], rate);
                float w = 1f / (1f + r * 0.5f);
                for (int i = 0; i < n; i++) y[i] += f.Run(src[i]) * w;
            }
            for (int i = 0; i < n; i++) y[i] *= amp(i / (float)n);
            Norm(y);
            return y;
        }

        /// <summary>A sine "bubble" whose pitch rises as it closes, normalised.</summary>
        public static float[] Bubble(int rate, float hz, float seconds, float rise)
        {
            int n = Math.Max(2, (int)(seconds * rate));
            var b = new float[n];
            float ph = 0f;
            float att = Math.Min(0.003f, seconds * 0.2f);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float u = i / (float)n;
                ph += hz * (1f + rise * u) / rate;
                b[i] = MathF.Sin(TwoPi * ph) * AD(t, att, seconds * 0.35f);
            }
            return b;
        }
        #endregion

        #region Voice
        /// <summary>
        /// A formant voice: a band-limited glottal sawtooth (or pure breath for a whisper) with vibrato, jitter,
        /// period-doubling growl and gurgle, through three parallel band-pass formants. Groans, roars, coughs,
        /// whispers and the bar's murmur all come out of this.
        /// </summary>
        public static float[] Voice(SynthGen g, float seconds, VoiceParams p)
        {
            int rate = g.Rate;
            int n = Math.Max(1, (int)(seconds * rate));
            var y = new float[n];
            var f1 = new Biquad();
            var f2 = new Biquad();
            var f3 = new Biquad();
            OnePole tilt = OnePole.At(p.Tilt, rate);
            OnePole jit = OnePole.At(3f, rate);
            OnePole flut = OnePole.At(28f, rate);
            OnePole gurg = OnePole.At(22f, rate);
            float jitK = jit.NoiseNormaliser, flutK = flut.NoiseNormaliser, gurgK = gurg.NoiseNormaliser;
            float phase = 0f, sub = 0f, dt = 1f / rate;
            float vibPh = g.Next();
            for (int i = 0; i < n; i++)
            {
                float u = i / (float)n;
                if ((i & 31) == 0)
                {
                    Vowel v = p.Formants(u);
                    f1.SetBP(v.F1, v.F1 / Math.Max(20f, v.B1), rate);
                    f2.SetBP(v.F2, v.F2 / Math.Max(20f, v.B2), rate);
                    f3.SetBP(v.F3, v.F3 / Math.Max(20f, v.B3), rate);
                }
                float amp = p.Amp(u);
                vibPh += p.VibratoHz * dt;
                float wander = p.Jitter * jitK * jit.Lp(g.Bipolar());
                float f0 = p.F0(u) * (1f + p.VibratoDepth * MathF.Sin(TwoPi * vibPh) + Math.Max(-0.5f, Math.Min(0.5f, wander)));
                float step = Math.Max(1f, f0) * dt;
                phase += step;
                if (phase >= 1f)
                {
                    phase -= 1f;
                    sub = 1f - sub;
                }

                float src = 0f;
                if (!p.Whisper)
                {
                    float voiced = tilt.Lp(Saw(phase, step));
                    if (p.Rough > 0f)
                    {
                        float doubling = 1f - p.Rough * 0.55f * sub;
                        float flutter = 1f + p.Rough * 0.3f * flutK * flut.Lp(g.Bipolar());
                        voiced *= doubling * Math.Max(0f, flutter);
                    }
                    src = voiced;
                }
                src += g.Bipolar() * p.Breath * (p.Whisper ? 1f : (phase < 0.45f ? 1f : 0.45f));
                if (p.Gurgle > 0f)
                {
                    float gl = gurg.Lp(g.Bipolar()) * gurgK;
                    src *= Math.Max(0f, 1f - p.Gurgle + p.Gurgle * gl * gl);
                }
                float o = p.G1 * f1.Run(src) + p.G2 * f2.Run(src) + p.G3 * f3.Run(src) + p.Direct * src;
                y[i] = o * amp;
            }
            return y;
        }
        #endregion

        #region Effects
        /// <summary>Soft saturation: normalises to unit peak, then tanh-drives (drive 1 = gentle, 4 = crushed).</summary>
        public static void Drive(float[] x, float drive)
        {
            Norm(x);
            if (drive <= 0.01f) return;
            float k = 1f / MathF.Tanh(drive);
            for (int i = 0; i < x.Length; i++) x[i] = MathF.Tanh(x[i] * drive) * k;
        }

        /// <summary>Feedback echo with a lowpass in the loop (darker repeats). Circular runs it twice so it wraps a loop.</summary>
        public static void Echo(float[] x, int rate, float delay, float feedback, float lpHz, float mix, bool circular = false)
        {
            int d = Math.Max(1, (int)(delay * rate));
            var line = new float[d];
            OnePole lp = OnePole.At(lpHz, rate);
            int idx = 0;
            int passes = circular ? 2 : 1;
            var wet = new float[x.Length];
            for (int pass = 0; pass < passes; pass++)
            {
                for (int i = 0; i < x.Length; i++)
                {
                    float o = line[idx];
                    line[idx] = x[i] + lp.Lp(o) * feedback;
                    if (++idx >= d) idx = 0;
                    if (pass == passes - 1) wet[i] = o;
                }
            }
            for (int i = 0; i < x.Length; i++) x[i] += wet[i] * mix;
        }

        private static readonly int[] s_Combs = { 1116, 1188, 1277, 1356, 1422, 1491 };
        private static readonly int[] s_Allpasses = { 556, 441, 341 };

        /// <summary>
        /// A small Freeverb-style room (six damped combs, three allpasses). The wet signal is scaled to
        /// <paramref name="wet"/> times the dry RMS. Circular processing wraps the tail round a loop.
        /// </summary>
        public static void Reverb(float[] x, int rate, float size, float damp, float wet, float dry = 1f, bool circular = false)
        {
            float scale = rate / 44100f * (0.75f + 0.5f * size);
            float fb = 0.70f + 0.27f * Clamp01(size);
            int nc = s_Combs.Length, na = s_Allpasses.Length;
            var cb = new float[nc][];
            var ci = new int[nc];
            var cs = new float[nc];
            for (int k = 0; k < nc; k++) cb[k] = new float[Math.Max(1, (int)(s_Combs[k] * scale))];
            var ab = new float[na][];
            var ai = new int[na];
            for (int k = 0; k < na; k++) ab[k] = new float[Math.Max(1, (int)(s_Allpasses[k] * scale))];

            int n = x.Length;
            var o = new float[n];
            int passes = circular ? 2 : 1;
            for (int pass = 0; pass < passes; pass++)
            {
                for (int i = 0; i < n; i++)
                {
                    float input = x[i] * 0.05f;
                    float acc = 0f;
                    for (int k = 0; k < nc; k++)
                    {
                        float[] b = cb[k];
                        int j = ci[k];
                        float y = b[j];
                        cs[k] = y * (1f - damp) + cs[k] * damp;
                        b[j] = input + cs[k] * fb;
                        ci[k] = ++j >= b.Length ? 0 : j;
                        acc += y;
                    }
                    for (int k = 0; k < na; k++)
                    {
                        float[] b = ab[k];
                        int j = ai[k];
                        float bo = b[j];
                        float y = bo - acc;
                        b[j] = acc + bo * 0.5f;
                        ai[k] = ++j >= b.Length ? 0 : j;
                        acc = y;
                    }
                    if (pass == passes - 1) o[i] = acc;
                }
            }
            float rd = Rms(x), rw = Rms(o);
            float k2 = rw > 1e-9f ? wet * rd / rw : 0f;
            for (int i = 0; i < n; i++) x[i] = x[i] * dry + o[i] * k2;
        }
        #endregion

        #region Levels and loops
        public static float Peak(float[] x)
        {
            float p = 0f;
            for (int i = 0; i < x.Length; i++)
            {
                float a = Math.Abs(x[i]);
                if (a > p) p = a;
            }
            return p;
        }

        public static float Rms(float[] x)
        {
            double s = 0;
            for (int i = 0; i < x.Length; i++) s += x[i] * x[i];
            return x.Length > 0 ? (float)Math.Sqrt(s / x.Length) : 0f;
        }

        public static void Norm(float[] x, float target = 1f)
        {
            float p = Peak(x);
            if (p < 1e-9f) return;
            float k = target / p;
            for (int i = 0; i < x.Length; i++) x[i] *= k;
        }

        public static void Gain(float[] x, float k)
        {
            for (int i = 0; i < x.Length; i++) x[i] *= k;
        }

        public static void Sanitize(float[] x)
        {
            for (int i = 0; i < x.Length; i++)
                if (float.IsNaN(x[i]) || float.IsInfinity(x[i])) x[i] = 0f;
        }

        public static void RemoveMean(float[] x)
        {
            double s = 0;
            for (int i = 0; i < x.Length; i++) s += x[i];
            float m = x.Length > 0 ? (float)(s / x.Length) : 0f;
            for (int i = 0; i < x.Length; i++) x[i] -= m;
        }

        public static void FadeIn(float[] x, int n)
        {
            n = Math.Min(n, x.Length);
            for (int i = 0; i < n; i++)
            {
                float w = MathF.Sin(0.5f * Pi * i / n);
                x[i] *= w * w;
            }
        }

        public static void FadeOut(float[] x, int n)
        {
            n = Math.Min(n, x.Length);
            int len = x.Length;
            for (int i = 0; i < n; i++)
            {
                float w = MathF.Sin(0.5f * Pi * i / n);
                x[len - 1 - i] *= w * w;
            }
        }

        /// <summary>The length once trailing near-silence (below threshold x peak) is dropped.</summary>
        public static int AudibleLength(float[] x, float threshold)
        {
            float lim = Peak(x) * threshold;
            for (int i = x.Length - 1; i > 0; i--)
                if (Math.Abs(x[i]) > lim) return Math.Min(x.Length, i + 1);
            return x.Length;
        }

        /// <summary>
        /// Makes a seamless loop of <paramref name="loopLen"/> samples from a longer render: the overhang past the
        /// end is cross-faded (equal power) into the start, so the last sample flows straight into the first.
        /// </summary>
        public static float[] CrossfadeLoop(float[] x, int loopLen)
        {
            int fade = Math.Min(x.Length - loopLen, loopLen);
            var o = new float[loopLen];
            Array.Copy(x, o, loopLen);
            for (int i = 0; i < fade; i++)
            {
                float w = 0.5f * Pi * i / fade;
                o[i] = x[i] * MathF.Sin(w) + x[loopLen + i] * MathF.Cos(w);
            }
            return o;
        }

        /// <summary>Rotates a loop so it starts on a zero crossing in its quietest stretch: a loop started (or used as
        /// a one-shot) then begins without a click.</summary>
        public static void RotateToQuiet(float[] x, int rate)
        {
            int n = x.Length;
            if (n < 64) return;
            int win = Math.Max(16, Math.Min(n / 8, rate / 50));
            double e = 0;
            for (int i = 0; i < win; i++) e += x[i] * x[i];
            double best = e;
            int bestAt = 0;
            for (int i = 1; i < n; i++)
            {
                float add = x[(i + win - 1) % n], drop = x[i - 1];
                e += add * add - drop * drop;
                if (e < best) { best = e; bestAt = i; }
            }
            int start = (bestAt + win / 2) % n;
            int zc = start;
            for (int k = 0; k < win; k++)
            {
                int a = (start + k) % n, b = (a + 1) % n;
                if ((x[a] <= 0f && x[b] > 0f) || (x[a] >= 0f && x[b] < 0f))
                {
                    zc = Math.Abs(x[a]) < Math.Abs(x[b]) ? a : b;
                    break;
                }
            }
            if (zc == 0) return;
            var tmp = new float[n];
            for (int i = 0; i < n; i++) tmp[i] = x[(zc + i) % n];
            Array.Copy(tmp, x, n);
        }
        #endregion
    }
}
