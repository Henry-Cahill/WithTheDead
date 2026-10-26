using System;

namespace CloneZ
{
    /// <summary>A stereo render: two channels of equal length (beds and music; positional sounds stay mono).</summary>
    internal sealed class StereoBuf
    {
        public readonly float[] L, R;

        public StereoBuf(int n)
        {
            L = new float[Math.Max(1, n)];
            R = new float[Math.Max(1, n)];
        }

        public StereoBuf(float[] l, float[] r)
        {
            L = l ?? new float[1];
            R = r ?? (float[])L.Clone();
        }

        public int Length => L.Length;

        /// <summary>Both channels a copy of <paramref name="mono"/>.</summary>
        public static StereoBuf Dual(float[] mono) => new StereoBuf(mono, (float[])mono.Clone());
    }

    /// <summary>Stereo, dynamics and envelope helpers that build on the core DSP kit.</summary>
    internal static partial class Dsp
    {
        #region Stereo
        /// <summary>Equal-power gains for <paramref name="pan"/> from -1 (hard left) to +1 (hard right).</summary>
        public static void PanGains(float pan, out float left, out float right)
        {
            float a = (Math.Max(-1f, Math.Min(1f, pan)) + 1f) * 0.25f * Pi;
            left = MathF.Cos(a);
            right = MathF.Sin(a);
        }

        /// <summary>Adds a mono source into a stereo buffer at sample <paramref name="at"/>, panned.</summary>
        public static void MixPan(StereoBuf dst, float[] src, int at, float gain, float pan)
        {
            PanGains(pan, out float l, out float r);
            Mix(dst.L, src, at, gain * l);
            Mix(dst.R, src, at, gain * r);
        }

        /// <summary>Adds a mono source into a stereo loop at sample <paramref name="at"/>, panned, wrapping round its end.</summary>
        public static void MixWrapPan(StereoBuf loop, float[] src, int at, float gain, float pan)
        {
            PanGains(pan, out float l, out float r);
            MixWrap(loop.L, src, at, gain * l);
            MixWrap(loop.R, src, at, gain * r);
        }

        /// <summary>Mid/side width: 0 collapses to mono, 1 leaves it, up to 2 exaggerates the sides.</summary>
        public static void Width(StereoBuf s, float width)
        {
            for (int i = 0; i < s.Length; i++)
            {
                float m = 0.5f * (s.L[i] + s.R[i]);
                float d = 0.5f * (s.L[i] - s.R[i]) * width;
                s.L[i] = m + d;
                s.R[i] = m - d;
            }
        }

        /// <summary>
        /// Two decorrelated copies of a mono signal (a short chain of seeded allpasses per side): a tonal mono source
        /// spread across the stereo field without the comb-filter colour of a plain delay. Amount 0..1 blends in the dry.
        /// </summary>
        public static StereoBuf Decorrelate(float[] x, SynthGen g, float amount = 1f)
        {
            var s = new StereoBuf((float[])x.Clone(), (float[])x.Clone());
            AllpassChain(s.L, g, amount);
            AllpassChain(s.R, g, amount);
            return s;
        }

        private static void AllpassChain(float[] x, SynthGen g, float amount)
        {
            var dry = (float[])x.Clone();
            for (int stage = 0; stage < 3; stage++)
            {
                int d = Math.Max(1, (int)(g.Range(0.003f, 0.017f) * g.Rate));
                float k = g.Range(0.45f, 0.7f);
                var line = new float[d];
                int j = 0;
                for (int i = 0; i < x.Length; i++)
                {
                    float bo = line[j];
                    float y = bo - k * x[i];
                    line[j] = x[i] + k * y;
                    x[i] = y;
                    if (++j >= d) j = 0;
                }
            }
            for (int i = 0; i < x.Length; i++) x[i] = Lerp(dry[i], x[i], Clamp01(amount));
        }

        private static readonly int[] s_StereoCombs = { 1116, 1188, 1277, 1356, 1422, 1491, 1557, 1617 };
        private static readonly int[] s_StereoAllpasses = { 556, 441, 341, 225 };

        /// <summary>
        /// A stereo Freeverb-style space: eight damped combs and four allpasses per side, the right side detuned by the
        /// classic spread so the tail is wide. Input is the mid of the buffer, optionally pre-delayed. The wet signal is
        /// scaled to <paramref name="wet"/> times the dry RMS. Circular processing wraps the tail round a loop.
        /// </summary>
        public static void StereoReverb(StereoBuf s, int rate, float size, float damp, float wet, float dry = 1f, float predelay = 0f, bool circular = false)
        {
            int n = s.Length;
            float scale = rate / 44100f * (0.75f + 0.5f * Clamp01(size));
            float fb = 0.70f + 0.27f * Clamp01(size);
            int spread = Math.Max(1, (int)(23 * rate / 44100f));
            Tank left = new Tank(scale, 0), right = new Tank(scale, spread);
            int pre = Math.Max(0, (int)(predelay * rate));
            var preLine = new float[Math.Max(1, pre)];
            int pj = 0;
            var wl = new float[n];
            var wr = new float[n];
            int passes = circular ? 2 : 1;
            for (int pass = 0; pass < passes; pass++)
            {
                for (int i = 0; i < n; i++)
                {
                    float input = 0.5f * (s.L[i] + s.R[i]) * 0.03f;
                    if (pre > 0)
                    {
                        float delayed = preLine[pj];
                        preLine[pj] = input;
                        if (++pj >= pre) pj = 0;
                        input = delayed;
                    }
                    float outL = left.Run(input, fb, damp);
                    float outR = right.Run(input, fb, damp);
                    if (pass == passes - 1) { wl[i] = outL; wr[i] = outR; }
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
                s.L[i] = s.L[i] * dry + wl[i] * k;
                s.R[i] = s.R[i] * dry + wr[i] * k;
            }
        }

        // One side of the stereo reverb: parallel damped combs into series allpasses.
        private sealed class Tank
        {
            private readonly float[][] _comb, _ap;
            private readonly int[] _ci, _ai;
            private readonly float[] _store;

            public Tank(float scale, int offset)
            {
                _comb = new float[s_StereoCombs.Length][];
                _ci = new int[_comb.Length];
                _store = new float[_comb.Length];
                for (int k = 0; k < _comb.Length; k++) _comb[k] = new float[Math.Max(1, (int)((s_StereoCombs[k] + offset) * scale))];
                _ap = new float[s_StereoAllpasses.Length][];
                _ai = new int[_ap.Length];
                for (int k = 0; k < _ap.Length; k++) _ap[k] = new float[Math.Max(1, (int)((s_StereoAllpasses[k] + offset) * scale))];
            }

            public float Run(float input, float fb, float damp)
            {
                float acc = 0f;
                for (int k = 0; k < _comb.Length; k++)
                {
                    float[] b = _comb[k];
                    int j = _ci[k];
                    float y = b[j];
                    _store[k] = y * (1f - damp) + _store[k] * damp;
                    b[j] = input + _store[k] * fb;
                    _ci[k] = ++j >= b.Length ? 0 : j;
                    acc += y;
                }
                for (int k = 0; k < _ap.Length; k++)
                {
                    float[] b = _ap[k];
                    int j = _ai[k];
                    float bo = b[j];
                    float y = bo - acc;
                    b[j] = acc + bo * 0.5f;
                    _ai[k] = ++j >= b.Length ? 0 : j;
                    acc = y;
                }
                return acc;
            }
        }
        #endregion

        #region Dynamics
        /// <summary>
        /// A feed-forward compressor/limiter (peak detector, dB domain): above <paramref name="thresholdDb"/> (re full
        /// scale) the level is reduced by <paramref name="ratio"/>; then <paramref name="makeupDb"/> is added. Gives
        /// transients body: a gunshot or a slam keeps its peak but gains loudness.
        /// </summary>
        public static void Compress(float[] x, int rate, float thresholdDb, float ratio, float attackMs, float releaseMs, float makeupDb = 0f)
            => Compress(x, null, rate, thresholdDb, ratio, attackMs, releaseMs, makeupDb);

        /// <summary>The stereo-linked <see cref="Compress(float[], int, float, float, float, float, float)"/> (both sides share one gain).</summary>
        public static void Compress(StereoBuf s, int rate, float thresholdDb, float ratio, float attackMs, float releaseMs, float makeupDb = 0f)
            => Compress(s.L, s.R, rate, thresholdDb, ratio, attackMs, releaseMs, makeupDb);

        private static void Compress(float[] a, float[] b, int rate, float thresholdDb, float ratio, float attackMs, float releaseMs, float makeupDb)
        {
            float att = MathF.Exp(-1f / Math.Max(1f, attackMs * 0.001f * rate));
            float rel = MathF.Exp(-1f / Math.Max(1f, releaseMs * 0.001f * rate));
            float slope = 1f - 1f / Math.Max(1f, ratio);
            float makeup = MathF.Pow(10f, makeupDb / 20f);
            float env = 0f;
            for (int i = 0; i < a.Length; i++)
            {
                float lvl = Math.Abs(a[i]);
                if (b != null && i < b.Length) lvl = Math.Max(lvl, Math.Abs(b[i]));
                env = lvl > env ? att * env + (1f - att) * lvl : rel * env + (1f - rel) * lvl;
                float db = 20f * MathF.Log10(env + 1e-9f);
                float over = db - thresholdDb;
                float gain = (over > 0f ? MathF.Pow(10f, -over * slope / 20f) : 1f) * makeup;
                a[i] *= gain;
                if (b != null && i < b.Length) b[i] *= gain;
            }
        }

        /// <summary>Soft saturation without renormalising (unlike <see cref="Drive"/>): tanh(x·drive)/drive keeps quiet parts level.</summary>
        public static void Saturate(float[] x, float drive)
        {
            if (drive <= 0.01f) return;
            float inv = 1f / drive;
            for (int i = 0; i < x.Length; i++) x[i] = MathF.Tanh(x[i] * drive) * inv;
        }
        #endregion

        #region Envelopes and resampling
        /// <summary>Piecewise-linear envelope through (time, level) breakpoints; 0 before the first, the last level after the last.</summary>
        public static float Env(float t, float[] times, float[] levels)
        {
            int n = Math.Min(times.Length, levels.Length);
            if (n == 0 || t < times[0]) return 0f;
            for (int i = 1; i < n; i++)
            {
                if (t < times[i])
                {
                    float span = times[i] - times[i - 1];
                    float u = span > 1e-9f ? (t - times[i - 1]) / span : 1f;
                    return Lerp(levels[i - 1], levels[i], u);
                }
            }
            return levels[n - 1];
        }

        /// <summary>Attack, decay to sustain level, held until <paramref name="gate"/> seconds, then a release.</summary>
        public static float ADSR(float t, float attack, float decay, float sustain, float gate, float release)
        {
            if (t < 0f) return 0f;
            float level;
            float tt = Math.Min(t, gate);
            if (tt < attack) level = tt / Math.Max(1e-6f, attack);
            else if (tt < attack + decay) level = Lerp(1f, sustain, (tt - attack) / Math.Max(1e-6f, decay));
            else level = sustain;
            if (t <= gate) return level;
            float r = (t - gate) / Math.Max(1e-6f, release);
            return r >= 1f ? 0f : level * (1f - r) * (1f - r);
        }

        /// <summary>The sample at fractional index <paramref name="idx"/> (linear interpolation; 0 outside the buffer).</summary>
        public static float ReadLerp(float[] x, float idx)
        {
            if (idx < 0f) return 0f;
            int i = (int)idx;
            if (i >= x.Length - 1) return i == x.Length - 1 ? x[i] : 0f;
            float f = idx - i;
            return x[i] + (x[i + 1] - x[i]) * f;
        }

        /// <summary>Plays <paramref name="x"/> back <paramref name="ratio"/> times faster (2 = an octave up, half as long).</summary>
        public static float[] Varispeed(float[] x, float ratio)
        {
            ratio = Math.Max(0.05f, ratio);
            int n = Math.Max(1, (int)(x.Length / ratio));
            var o = new float[n];
            for (int i = 0; i < n; i++) o[i] = ReadLerp(x, i * ratio);
            return o;
        }
        #endregion
    }
}
