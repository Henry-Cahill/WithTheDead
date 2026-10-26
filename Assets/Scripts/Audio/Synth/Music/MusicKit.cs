using System;

namespace CloneZ
{
    /// <summary>How a plucked string sounds: how long it rings, how fast its overtones die, how bright the pluck is,
    /// where along the string it is plucked and how much of the pick is heard.</summary>
    internal readonly struct StringTone
    {
        /// <summary>Seconds for the fundamental to die away by 60 dB (around G3; higher strings ring shorter).</summary>
        public readonly float T60;
        /// <summary>The loop filter's weight, 0.05 (bright, steel) .. 0.5 (dark, nylon or gut): how fast the overtones die.</summary>
        public readonly float Stretch;
        /// <summary>Low-pass on the pluck itself at full strength (Hz): a softer touch is darker.</summary>
        public readonly float Bright;
        /// <summary>Where the string is plucked (fraction of its length from the bridge).</summary>
        public readonly float Position;
        /// <summary>Level of the pick's own click.</summary>
        public readonly float Pick;

        public StringTone(float t60, float stretch, float bright, float position, float pick)
        {
            T60 = t60;
            Stretch = stretch;
            Bright = bright;
            Position = position;
            Pick = pick;
        }
    }

    /// <summary>How a bowed note sounds: the bowing (attack, release, bow hiss), vibrato, tremolo and how many players.</summary>
    internal readonly struct BowTone
    {
        public readonly float Attack, Release, Vibrato, VibratoHz, Noise, Bright, Tremolo, TremoloHz, Detune;
        public readonly int Voices;

        public BowTone(float attack, float release, float vibrato, float vibratoHz, float noise, float bright,
            int voices = 1, float detune = 0f, float tremolo = 0f, float tremoloHz = 0f)
        {
            Attack = attack;
            Release = release;
            Vibrato = vibrato;
            VibratoHz = vibratoHz;
            Noise = noise;
            Bright = bright;
            Voices = voices;
            Detune = detune;
            Tremolo = tremolo;
            TremoloHz = tremoloHz;
        }
    }

    /// <summary>
    /// The score's instruments and its room, engine-free (the music is rendered on background threads and by the offline
    /// sound tools): plucked strings (an extended Karplus-Strong string with a tuned fractional delay: guitar, banjo,
    /// mountain dulcimer, upright bass), bowed strings (fiddle, cello, tremolo sections, spiccato), drones whose phase is
    /// a function of absolute time so they carry on seamlessly from one segment of the score into the next, drums (taiko,
    /// toms, a heartbeat, brushes, clicks), a harmonica reed, a wordless choir, and a linear stereo room (a feedback delay
    /// network) whose tails overlap-add exactly across segments. Every function adds into caller-owned buffers.
    /// </summary>
    internal static class MusicKit
    {
        public const float TwoPi = Dsp.TwoPi;

        #region Instruments
        public static readonly StringTone Guitar = new StringTone(3.4f, 0.42f, 4200f, 0.17f, 0.35f);
        public static readonly StringTone Banjo = new StringTone(1.15f, 0.2f, 7600f, 0.1f, 0.75f);
        public static readonly StringTone Dulcimer = new StringTone(4.4f, 0.3f, 5400f, 0.13f, 0.45f);
        public static readonly StringTone Bass = new StringTone(1.9f, 0.5f, 1100f, 0.22f, 0.12f);
        public static readonly StringTone Muted = new StringTone(0.3f, 0.5f, 1500f, 0.2f, 0.3f);
        public static readonly StringTone Rhythm = new StringTone(1.4f, 0.4f, 3600f, 0.2f, 0.45f);

        public static readonly BowTone Fiddle = new BowTone(0.12f, 0.28f, 0.0048f, 5.7f, 0.07f, 3600f);
        public static readonly BowTone FiddleSwell = new BowTone(1.1f, 1.5f, 0.0042f, 5.2f, 0.05f, 2300f, 2, 9f);
        public static readonly BowTone Cello = new BowTone(1.3f, 1.7f, 0.0032f, 4.8f, 0.05f, 1500f, 2, 7f);
        public static readonly BowTone Section = new BowTone(1.8f, 2.2f, 0.0028f, 5.1f, 0.06f, 2400f, 3, 13f);
        public static readonly BowTone Ponticello = new BowTone(1.6f, 1.8f, 0.0015f, 5f, 0.13f, 6200f, 3, 16f, 0.6f, 10.5f);
        public static readonly BowTone Spiccato = new BowTone(0.004f, 0.05f, 0f, 5f, 0.12f, 1100f, 2, 12f);
        #endregion

        #region Plucked strings
        /// <summary>
        /// One plucked note added into <paramref name="dst"/> at sample <paramref name="at"/>: a noise burst shaped by the
        /// touch and the plucking point, circulating in a tuned, damped delay line (two-point loss filter plus a first-order
        /// allpass for the fractional part of the period, so every pitch is in tune). <paramref name="ring"/> seconds after
        /// the pluck the string is damped (the next note on it, a palm), dying away over <paramref name="dampT60"/>.
        /// </summary>
        public static void Pluck(float[] dst, int at, int rate, float hz, float amp, in StringTone tone, SynthGen g, float ring = 30f, float dampT60 = 0.12f)
        {
            if (dst == null || amp <= 0f || hz < 20f || hz > rate * 0.2f || at >= dst.Length) return;
            float period = rate / hz;
            float s = Math.Max(0.05f, Math.Min(0.5f, tone.Stretch));
            int n = Math.Max(2, (int)(period - s - 0.1f));
            float frac = period - s - n;
            float c = (1f - frac) / (1f + frac);
            float w0 = TwoPi * hz / rate;
            float h = MathF.Sqrt(Math.Max(1e-6f, 1f - 2f * s * (1f - s) * (1f - MathF.Cos(w0))));
            float t60 = tone.T60 * MathF.Pow(Math.Max(0.3f, hz / 196f), -0.3f);
            float rho = Math.Min(0.99997f, MathF.Pow(10f, -3f / (t60 * hz)) / h);
            float rhoDamp = Math.Min(rho, MathF.Pow(10f, -3f / (Math.Max(0.02f, dampT60) * hz)) / h);

            // The pluck: low-passed noise (darker for a softer touch), less its mean, through the plucking point's comb.
            var line = new float[n];
            OnePole lp = OnePole.At(tone.Bright * (0.3f + 0.7f * Dsp.Clamp01(amp)), rate);
            double mean = 0;
            for (int i = 0; i < n; i++)
            {
                line[i] = lp.Lp(lp.Lp(g.Bipolar()));
                mean += line[i];
            }
            float m = (float)(mean / n);
            for (int i = 0; i < n; i++) line[i] -= m;
            int pos = Math.Max(1, Math.Min(n - 1, (int)(tone.Position * n + 0.5f)));
            for (int i = n - 1; i >= pos; i--) line[i] -= line[i - pos];
            float peak = 1e-9f;
            for (int i = 0; i < n; i++) peak = Math.Max(peak, Math.Abs(line[i]));
            float k = amp / peak;
            for (int i = 0; i < n; i++) line[i] *= k;

            long dampAt = at + (long)(Math.Max(0f, ring) * rate);
            float z = 0f, apIn = 0f, apOut = 0f, watch = 0f, floor = amp * 2e-4f;
            int idx = 0, count = 0;
            for (int j = at; j < dst.Length; j++)
            {
                float y = line[idx];
                if (j >= 0) dst[j] += y;
                float avg = (1f - s) * y + s * z;
                z = y;
                float ap = c * avg + apIn - c * apOut;
                apIn = avg;
                apOut = ap;
                line[idx] = (j < dampAt ? rho : rhoDamp) * ap;
                if (++idx >= n) idx = 0;
                float a = y < 0f ? -y : y;
                if (a > watch) watch = a;
                if (++count >= 2048)
                {
                    if (watch < floor) break;
                    watch = 0f;
                    count = 0;
                }
            }

            if (tone.Pick <= 0f) return;
            OnePole hp = OnePole.At(1600f, rate);
            float e = amp * tone.Pick * 0.35f, d = MathF.Exp(-1f / (0.0016f * rate));
            int len = Math.Min(rate / 100, dst.Length - at);
            for (int i = 0; i < len; i++)
            {
                if (at + i >= 0) dst[at + i] += hp.Hp(g.Bipolar()) * e;
                e *= d;
            }
        }

        /// <summary>A strummed chord: the strings plucked one after another (low to high on a downstroke).</summary>
        public static void Strum(float[] dst, int at, int rate, float[] hz, float amp, in StringTone tone, SynthGen g, bool down, float spread = 0.011f, float ring = 30f, float dampT60 = 0.1f)
        {
            int count = hz.Length;
            for (int i = 0; i < count; i++)
            {
                int s = down ? i : count - 1 - i;
                int t = at + (int)(i * spread * rate * g.Range(0.75f, 1.25f));
                float a = amp * g.Range(0.75f, 1f) * (down ? 1f - 0.06f * i : 0.8f + 0.04f * i);
                Pluck(dst, t, rate, hz[s], a, tone, g, ring - i * spread, dampT60);
            }
        }

        /// <summary>A natural harmonic or a struck bell-like tone: a sine and a faint octave, ringing away.</summary>
        public static void Harmonic(float[] dst, int at, int rate, float hz, float amp, float decay, SynthGen g)
        {
            int len = Math.Min(dst.Length - at, (int)(decay * 6.5f * rate));
            float dt = 1f / rate;
            float k = MathF.Exp(-dt / Math.Max(0.01f, decay)), k2 = MathF.Exp(-dt / Math.Max(0.01f, decay * 0.4f));
            float e = 1f, e2 = 1f;
            double ph = g.Next(), ph2 = g.Next();
            float inc = hz * dt, inc2 = hz * 2.003f * dt;
            int attack = Math.Max(1, (int)(0.004f * rate));
            for (int i = 0; i < len; i++)
            {
                ph += inc;
                if (ph >= 1.0) ph -= 1.0;
                ph2 += inc2;
                if (ph2 >= 1.0) ph2 -= 1.0;
                float v = MathF.Sin(TwoPi * (float)ph) * e + 0.18f * MathF.Sin(TwoPi * (float)ph2) * e2;
                if (i < attack) v *= i / (float)attack;
                int j = at + i;
                if (j >= 0) dst[j] += v * amp;
                e *= k;
                e2 *= k2;
            }
        }
        #endregion

        #region Bowed strings
        /// <summary>
        /// One bowed note (fiddle, cello, a section): band-limited sawtooth players with delayed vibrato, slow pitch
        /// drift, bow hiss locked to the stick-slip, optional tremolo bowing, a slide in from <paramref name="fromHz"/>
        /// and a glide over the whole note toward <paramref name="toHz"/>. <paramref name="seconds"/> includes the release.
        /// </summary>
        public static void Bow(float[] dst, int at, int rate, float hz, float amp, float seconds, in BowTone t, SynthGen g,
            float fromHz = 0f, float slide = 0.12f, float toHz = 0f)
        {
            int len = (int)(seconds * rate);
            if (dst == null || len <= 0 || at >= dst.Length || hz <= 0f) return;
            int voices = Math.Max(1, t.Voices);
            float norm = amp / MathF.Sqrt(voices), dt = 1f / rate;
            int attN = Math.Max(1, (int)(Math.Max(0.003f, Math.Min(t.Attack, seconds * 0.5f)) * rate));
            int relN = Math.Max(1, (int)(Math.Max(0.01f, Math.Min(t.Release, seconds * 0.5f)) * rate));
            int relAt = len - relN;
            int vibAt = (int)(Math.Min(0.35f, seconds * 0.3f) * rate), vibN = Math.Max(1, (int)(0.45f * rate));
            float glideK = toHz > 0f ? MathF.Exp(MathF.Log(toHz / hz) / Math.Max(1, len)) : 1f;
            float slideLog = fromHz > 0f ? MathF.Log(fromHz / hz) : 0f;
            int slideN = fromHz > 0f ? Math.Max(1, (int)(slide * rate)) : 0;
            float a1 = MathF.Exp(-TwoPi * Math.Min(t.Bright, rate * 0.45f) / rate), a2 = MathF.Exp(-TwoPi * Math.Min(t.Bright * 1.6f, rate * 0.45f) / rate);
            float ah = MathF.Exp(-TwoPi * 1200f / rate), ad = MathF.Exp(-TwoPi * 2.2f / rate);
            float driftK = 0.0012f / MathF.Sqrt(Math.Max(1e-12f, (1f / 3f) * (1f - ad) / (1f + ad)));
            float depth = t.Vibrato, noiseAmt = t.Noise, tremDepth = t.Tremolo * 0.5f;
            bool trem = t.Tremolo > 0f;
            int end = Math.Min(len, dst.Length - at);
            for (int v = 0; v < voices; v++)
            {
                float cents = voices > 1 ? (v / (float)(voices - 1) - 0.5f) * t.Detune + g.Bipolar() * t.Detune * 0.12f : 0f;
                float baseF = hz * MathF.Pow(2f, cents / 1200f);
                // Vibrato and tremolo as rotating phasors (no sine per sample); white noise from an inline xorshift.
                float w = TwoPi * t.VibratoHz * g.Range(0.9f, 1.1f) / rate, vc = MathF.Cos(w), vs = MathF.Sin(w);
                float p0 = g.Next() * TwoPi, vr = MathF.Cos(p0), vi = MathF.Sin(p0);
                w = TwoPi * t.TremoloHz * g.Range(0.85f, 1.15f) / rate;
                float tc = MathF.Cos(w), ts = MathF.Sin(w);
                p0 = g.Next() * TwoPi;
                float tr = MathF.Cos(p0), ti = MathF.Sin(p0);
                uint rs = g.NextUInt() | 1u;
                double ph = g.Next();
                float z1 = 0f, z2 = 0f, zh = 0f, zd = 0f, glide = 1f;
                int start = Math.Max(0, -at) + (v == 0 ? 0 : (int)(g.Range(0f, 0.03f) * rate));
                for (int i = start; i < end; i++)
                {
                    float env = 1f;
                    if (i < attN)
                    {
                        float u = i / (float)attN;
                        env = u * u * (3f - 2f * u);
                    }
                    if (i > relAt)
                    {
                        float u = Math.Min(1f, (i - relAt) / (float)relN);
                        env *= 1f - u * u * (3f - 2f * u);
                    }
                    float nr = vr * vc - vi * vs;
                    vi = vr * vs + vi * vc;
                    vr = nr;
                    float vib = depth * vi;
                    if (i < vibAt) vib = 0f;
                    else if (i < vibAt + vibN)
                    {
                        float u = (i - vibAt) / (float)vibN;
                        vib *= u * u * (3f - 2f * u);
                    }
                    rs ^= rs << 13;
                    rs ^= rs >> 17;
                    rs ^= rs << 5;
                    float white = (int)rs * 4.656613e-10f;
                    zd = white + (zd - white) * ad;
                    float f = baseF * glide;
                    glide *= glideK;
                    if (i < slideN)
                    {
                        float u = i / (float)slideN;
                        f *= MathF.Exp(slideLog * (1f - u * u * (3f - 2f * u)));
                    }
                    f *= 1f + vib + zd * driftK;
                    float step = f * dt;
                    ph += step;
                    if (ph >= 1.0) ph -= 1.0;
                    float p = (float)ph, saw = 2f * p - 1f;
                    if (p < step)
                    {
                        float x = p / step;
                        saw -= x + x - x * x - 1f;
                    }
                    else if (p > 1f - step)
                    {
                        float x = (p - 1f) / step;
                        saw -= x * x + x + x + 1f;
                    }
                    z1 = saw + (z1 - saw) * a1;
                    z2 = z1 + (z2 - z1) * a2;
                    zh = white + (zh - white) * ah;
                    float hiss = (white - zh) * noiseAmt * (p < 0.5f ? 1f : 0.3f);
                    float gain = env * norm;
                    if (trem)
                    {
                        float nt = tr * tc - ti * ts;
                        ti = tr * ts + ti * tc;
                        tr = nt;
                        gain *= 1f - tremDepth * (1f + ti);
                    }
                    dst[at + i] += (z2 + hiss) * gain;
                    if ((i & 2047) == 0)
                    {
                        float m = 1f / MathF.Sqrt(vr * vr + vi * vi);
                        vr *= m;
                        vi *= m;
                        m = 1f / MathF.Sqrt(tr * tr + ti * ti);
                        tr *= m;
                        ti *= m;
                    }
                }
            }
        }

        /// <summary>
        /// A drone that carries on across segments: each voice's phase and its slow swell are functions of absolute time
        /// (<paramref name="startAbs"/> is the absolute time of sample 0), so two segments rendering the same drone agree
        /// sample for sample where they overlap. Windowed: a linear fade-in over [0, fadeIn], held to
        /// <paramref name="holdUntil"/>, a linear fade-out over <paramref name="fadeOut"/> — neighbouring segments'
        /// windows sum to one, so the seam is exact. Deterministic (no generator): all its colour comes from the caller.
        /// </summary>
        public static void Drone(StereoBuf s, int rate, double startAbs, float hz, float amp, float bright, int voices, float detuneCents,
            float width, float swellHz, float swellPhase, float swellDepth, float fadeIn, float holdUntil, float fadeOut)
        {
            int len = Math.Min(s.Length, (int)((holdUntil + fadeOut) * rate) + 1);
            voices = Math.Max(1, voices);
            float norm = amp / MathF.Sqrt(voices);
            double dtd = 1.0 / rate;
            for (int v = 0; v < voices; v++)
            {
                float u = voices > 1 ? v / (float)(voices - 1) : 0.5f;
                double f = hz * Math.Pow(2.0, (u - 0.5) * detuneCents / 1200.0);
                double ph = f * startAbs;
                ph -= Math.Floor(ph);
                double inc = f * dtd;
                float step = (float)inc;
                Dsp.PanGains((u * 2f - 1f) * width, out float gl, out float gr);
                float ka = MathF.Exp(-TwoPi * Math.Min(bright, rate * 0.45f) / rate), kb = MathF.Exp(-TwoPi * Math.Min(bright * 1.4f, rate * 0.45f) / rate);
                float za = 0f, zb = 0f;
                float sp = swellPhase + v * 0.37f, swell = 1f;
                float[] left = s.L, right = s.R;
                float invIn = 1f / Math.Max(1e-4f, fadeIn), invOut = 1f / Math.Max(1e-4f, fadeOut);
                for (int i = 0; i < len; i++)
                {
                    ph += inc;
                    if (ph >= 1.0) ph -= 1.0;
                    float p = (float)ph, saw = 2f * p - 1f;
                    if (p < step)
                    {
                        float x = p / step;
                        saw -= x + x - x * x - 1f;
                    }
                    else if (p > 1f - step)
                    {
                        float x = (p - 1f) / step;
                        saw -= x * x + x + x + 1f;
                    }
                    za = saw + (za - saw) * ka;
                    zb = za + (zb - za) * kb;
                    // The swell moves on a 32-sample control grid; segments start on multiples of 32, so the grids agree.
                    if ((i & 31) == 0)
                    {
                        double c = swellHz * (startAbs + i * dtd);
                        swell = 1f - swellDepth * (0.5f + 0.5f * MathF.Sin(TwoPi * ((float)(c - Math.Floor(c)) + sp)));
                    }
                    float time = (float)(i * dtd);
                    float w = time < fadeIn ? time * invIn : time <= holdUntil ? 1f : Math.Max(0f, 1f - (time - holdUntil) * invOut);
                    float o = zb * norm * w * swell;
                    left[i] += o * gl;
                    right[i] += o * gr;
                }
            }
        }

        /// <summary>Breath and bow hiss under a drone: band-passed noise with the drone's window shaped for equal power
        /// (the noise of neighbouring segments is uncorrelated, so their powers, not their amplitudes, must add to one).</summary>
        public static void DroneAir(StereoBuf s, int rate, SynthGen g, float amp, float hz, float q, float fadeIn, float holdUntil, float fadeOut)
        {
            int len = Math.Min(s.Length, (int)((holdUntil + fadeOut) * rate) + 1);
            Biquad bl = Biquad.BP(hz, q, rate), br = Biquad.BP(hz * 1.07f, q, rate);
            float dt = 1f / rate, k = amp * (1f + q);
            for (int i = 0; i < len; i++)
            {
                float time = i * dt;
                float w = time < fadeIn ? time / fadeIn : time <= holdUntil ? 1f : Math.Max(0f, 1f - (time - holdUntil) / fadeOut);
                w = MathF.Sqrt(w);
                s.L[i] += bl.Run(g.Bipolar()) * k * w;
                s.R[i] += br.Run(g.Bipolar()) * k * w;
            }
        }
        #endregion

        #region Drums
        /// <summary>
        /// A drum: a sine body whose pitch falls from <paramref name="hz0"/> to <paramref name="hz1"/>, a band of skin noise
        /// and a stick click. Taiko, toms, the heartbeat and the deep booms are all this with different numbers.
        /// </summary>
        public static void Drum(float[] dst, int at, int rate, float amp, float hz0, float hz1, float glide, float decay,
            float noise, float noiseHz, float noiseQ, float noiseDecay, float click, SynthGen g)
        {
            if (dst == null || at >= dst.Length || amp <= 0f) return;
            float dt = 1f / rate;
            int len = Math.Min(dst.Length - at, (int)(decay * 6.2f * rate));
            float env = 1f, kEnv = MathF.Exp(-dt / Math.Max(0.005f, decay));
            float gl = 1f, kGl = MathF.Exp(-dt / Math.Max(0.002f, glide));
            int attack = Math.Max(1, (int)(0.0012f * rate));
            // The body as a rotating phasor (small-angle steps: drums sit far below the sample rate).
            float re = 1f, im = 0f;
            for (int i = 0; i < len; i++)
            {
                float w = TwoPi * (hz1 + (hz0 - hz1) * gl) * dt;
                gl *= kGl;
                float w2 = w * w, c = 1f - 0.5f * w2 + w2 * w2 * (1f / 24f), sn = w - w * w2 * (1f / 6f);
                float nr = re * c - im * sn;
                im = re * sn + im * c;
                re = nr;
                float v = im * env;
                if (i < attack) v *= i / (float)attack;
                env *= kEnv;
                int j = at + i;
                if (j >= 0) dst[j] += v * amp;
                if ((i & 1023) == 1023)
                {
                    float m = 1f / MathF.Sqrt(re * re + im * im);
                    re *= m;
                    im *= m;
                }
            }
            if (noise > 0f)
            {
                Biquad bp = Biquad.BP(noiseHz, noiseQ, rate);
                float e = amp * noise * (1f + noiseQ), ke = MathF.Exp(-dt / Math.Max(0.002f, noiseDecay));
                int nlen = Math.Min(dst.Length - at, (int)(noiseDecay * 7f * rate));
                for (int i = 0; i < nlen; i++)
                {
                    float v = bp.Run(g.Bipolar()) * e;
                    e *= ke;
                    int j = at + i;
                    if (j >= 0) dst[j] += v;
                }
            }
            if (click > 0f)
            {
                OnePole hp = OnePole.At(2600f, rate);
                float e = amp * click, ke = MathF.Exp(-dt / 0.0018f);
                int clen = Math.Min(dst.Length - at, (int)(0.012f * rate));
                for (int i = 0; i < clen; i++)
                {
                    float v = hp.Hp(g.Bipolar()) * e;
                    e *= ke;
                    int j = at + i;
                    if (j >= 0) dst[j] += v;
                }
            }
        }

        /// <summary>A big taiko-like drum (<paramref name="size"/> 1 = large; smaller is higher and shorter).</summary>
        public static void Taiko(float[] dst, int at, int rate, float amp, float size, SynthGen g)
        {
            float p = g.Range(0.97f, 1.03f) / size;
            Drum(dst, at, rate, amp, 150f * p, 52f * p, 0.04f, 0.42f * size, 0.5f, 330f * p, 1.1f, 0.06f, 0.18f, g);
            Drum(dst, at, rate, amp * 0.32f, 240f * p, 86f * p, 0.03f, 0.16f * size, 0f, 0f, 1f, 0.01f, 0f, g);
        }

        /// <summary>A tom (<paramref name="pitch"/> 1 = a low floor tom).</summary>
        public static void Tom(float[] dst, int at, int rate, float amp, float pitch, SynthGen g)
        {
            float p = pitch * g.Range(0.98f, 1.02f);
            Drum(dst, at, rate, amp, 230f * p, 128f * p, 0.028f, 0.2f, 0.42f, 720f * p, 1.4f, 0.035f, 0.32f, g);
        }

        /// <summary>A heartbeat: a felt "lub" and a softer "dub" a fifth of a second later.</summary>
        public static void Heart(float[] dst, int at, int rate, float amp, SynthGen g)
        {
            Drum(dst, at, rate, amp, 74f, 45f, 0.022f, 0.1f, 0.18f, 140f, 1.2f, 0.03f, 0f, g);
            int dub = at + (int)(g.Range(0.2f, 0.24f) * rate);
            Drum(dst, dub, rate, amp * 0.62f, 66f, 43f, 0.02f, 0.085f, 0.14f, 130f, 1.2f, 0.025f, 0f, g);
        }

        /// <summary>A deep, distant boom (a far blast felt more than heard).</summary>
        public static void Boom(float[] dst, int at, int rate, float amp, SynthGen g)
        {
            Drum(dst, at, rate, amp, g.Range(62f, 70f), g.Range(29f, 33f), 0.09f, 1.1f, 0.45f, 90f, 0.7f, 0.5f, 0f, g);
        }

        /// <summary>A rim click / stick tick.</summary>
        public static void Tick(float[] dst, int at, int rate, float amp, SynthGen g)
        {
            Drum(dst, at, rate, amp * 0.25f, g.Range(1500f, 1750f), 1450f, 0.004f, 0.012f, 0.9f, 2600f, 2.4f, 0.01f, 0.3f, g);
        }

        /// <summary>A brush on a snare: a swish (<paramref name="swish"/> seconds) and/or a tap.</summary>
        public static void Brush(float[] dst, int at, int rate, float amp, float swish, float tap, SynthGen g)
        {
            if (dst == null || at >= dst.Length) return;
            float dt = 1f / rate;
            if (swish > 0f)
            {
                Biquad bp = Biquad.BP(g.Range(3600f, 4400f), 0.8f, rate);
                int len = Math.Min(dst.Length - at, (int)(swish * 2.2f * rate));
                float att = swish * 0.3f;
                for (int i = 0; i < len; i++)
                {
                    float time = i * dt;
                    float env = time < att ? Dsp.SmoothStep(time / att) : MathF.Exp(-(time - att) / (swish * 0.45f));
                    int j = at + i;
                    if (j >= 0) dst[j] += bp.Run(g.Bipolar()) * env * amp * 1.6f;
                }
            }
            if (tap > 0f)
            {
                Biquad bp = Biquad.BP(g.Range(2200f, 2800f), 1.1f, rate);
                float e = amp * tap * 2.2f, ke = MathF.Exp(-dt / 0.022f);
                int len = Math.Min(dst.Length - at, (int)(0.15f * rate));
                for (int i = 0; i < len; i++)
                {
                    int j = at + i;
                    if (j >= 0) dst[j] += bp.Run(g.Bipolar()) * e;
                    e *= ke;
                }
            }
        }

        /// <summary>A riser: noise swelling up a band toward <paramref name="seconds"/> after <paramref name="at"/>, cut
        /// short there (the next downbeat lands on the cut).</summary>
        public static void Riser(float[] dst, int at, int rate, float amp, float seconds, SynthGen g)
        {
            int len = Math.Min(dst.Length - at, (int)(seconds * rate));
            var bp = new Biquad();
            float dt = 1f / rate;
            int fade = Math.Max(1, (int)(0.006f * rate));
            for (int i = 0; i < len; i++)
            {
                float u = i / (float)len;
                if ((i & 15) == 0) bp.SetBP(500f * MathF.Pow(12f, u), 1.4f, rate);
                float env = u * u * u * (i > len - fade ? (len - i) / (float)fade : 1f);
                int j = at + i;
                if (j >= 0) dst[j] += bp.Run(g.Bipolar()) * env * amp * 2.2f;
            }
        }
        #endregion

        #region Winds and voices
        /// <summary>
        /// A harmonica reed: a narrow band-limited pulse with breath, a blues bend up from <paramref name="bendCents"/>
        /// below the note over <paramref name="bendTime"/>, and a hand tremolo.
        /// </summary>
        public static void Reed(float[] dst, int at, int rate, float hz, float amp, float seconds, float bendCents, float bendTime, SynthGen g)
        {
            int len = Math.Min(dst.Length - at, (int)(seconds * rate));
            if (len <= 0) return;
            float dt = 1f / rate;
            double ph = g.Next();
            float trem = g.Range(4.6f, 5.6f), tremPh = g.Next();
            OnePole breathHp = OnePole.At(1500f, rate), tone = OnePole.At(5200f, rate);
            float att = Math.Min(0.035f, seconds * 0.3f), rel = Math.Min(0.08f, seconds * 0.3f);
            for (int i = 0; i < len; i++)
            {
                float time = i * dt;
                float env = Dsp.SmoothStep(time / att) * (1f - Dsp.SmoothStep((time - (seconds - rel)) / rel));
                float cents = bendCents * (1f - Dsp.SmoothStep(time / Math.Max(0.01f, bendTime)));
                float f = hz * MathF.Pow(2f, cents / 1200f);
                float step = f * dt;
                ph += step;
                if (ph >= 1.0) ph -= 1.0;
                float p = (float)ph, q = p + 0.32f;
                if (q >= 1f) q -= 1f;
                float pulse = Dsp.Saw(p, step) - Dsp.Saw(q, step);
                float tr = 1f - 0.22f * (0.5f + 0.5f * MathF.Sin(TwoPi * (trem * time + tremPh))) * Dsp.SmoothStep((time - 0.15f) / 0.3f);
                float breath = breathHp.Hp(g.Bipolar()) * 0.09f;
                int j = at + i;
                if (j >= 0) dst[j] += (tone.Lp(pulse) + breath) * env * tr * amp;
            }
        }

        /// <summary>
        /// A wordless voice of a choir ("oo" opening to "ah"), slow and breathy: a band-limited glottal sawtooth with
        /// vibrato and breath, tilted, through three formant band-passes that morph with the vowel.
        /// </summary>
        public static void Choir(float[] dst, int at, int rate, float hz, float amp, float seconds, SynthGen g, float glideCents = 0f)
        {
            int total = (int)(seconds * rate);
            int len = Math.Min(total, dst.Length - at);
            if (len <= 0) return;
            float dt = 1f / rate;
            int attN = Math.Max(1, (int)(total * 0.35f)), relN = Math.Max(1, (int)(total * 0.35f)), relAt = total - relN;
            float w = TwoPi * g.Range(4.6f, 5.4f) / rate, vc = MathF.Cos(w), vs = MathF.Sin(w), vr = 1f, vi = 0f;
            float glideK = MathF.Exp(0.6931472f * glideCents / 1200f / Math.Max(1, total));
            float kt = MathF.Exp(-TwoPi * 1800f / rate), zt = 0f;
            float scale = hz > 200f ? 1.08f : 0.94f, f = hz;
            uint rs = g.NextUInt() | 1u;
            double ph = g.Next();
            float b0a = 0f, a1a = 0f, a2a = 0f, b0b = 0f, a1b = 0f, a2b = 0f, b0c = 0f, a1c = 0f, a2c = 0f;
            float z1a = 0f, z2a = 0f, z1b = 0f, z2b = 0f, z1c = 0f, z2c = 0f;
            int start = Math.Max(0, -at);
            for (int i = start; i < len; i++)
            {
                if (((i - start) & 63) == 0)
                {
                    float u = Dsp.SmoothStep(i / (float)total * 1.6f);
                    BandPass((300f + 270f * u) * scale, 70f + 20f * u, rate, out b0a, out a1a, out a2a);
                    BandPass((870f - 30f * u) * scale, 110f + 10f * u, rate, out b0b, out a1b, out a2b);
                    BandPass((2240f + 170f * u) * scale, 160f + 10f * u, rate, out b0c, out a1c, out a2c);
                }
                float nr = vr * vc - vi * vs;
                vi = vr * vs + vi * vc;
                vr = nr;
                rs ^= rs << 13;
                rs ^= rs >> 17;
                rs ^= rs << 5;
                float white = (int)rs * 4.656613e-10f;
                float step = f * (1f + 0.006f * vi) * dt;
                f *= glideK;
                ph += step;
                if (ph >= 1.0) ph -= 1.0;
                float p = (float)ph, saw = 2f * p - 1f;
                if (p < step)
                {
                    float x = p / step;
                    saw -= x + x - x * x - 1f;
                }
                else if (p > 1f - step)
                {
                    float x = (p - 1f) / step;
                    saw -= x * x + x + x + 1f;
                }
                zt = saw + (zt - saw) * kt;
                float src = zt + white * 0.22f * (p < 0.45f ? 1f : 0.45f);
                float ya = b0a * src + z1a;
                z1a = z2a - a1a * ya;
                z2a = -b0a * src - a2a * ya;
                float yb = b0b * src + z1b;
                z1b = z2b - a1b * yb;
                z2b = -b0b * src - a2b * yb;
                float yc = b0c * src + z1c;
                z1c = z2c - a1c * yc;
                z2c = -b0c * src - a2c * yc;
                float env = 1f;
                if (i < attN)
                {
                    float u = i / (float)attN;
                    env = u * u * (3f - 2f * u);
                }
                else if (i > relAt)
                {
                    float u = Math.Min(1f, (i - relAt) / (float)relN);
                    env = 1f - u * u * (3f - 2f * u);
                }
                dst[at + i] += (ya + 0.5f * yb + 0.18f * yc + 0.03f * src) * env * amp;
                if ((i & 2047) == 0)
                {
                    float m = 1f / MathF.Sqrt(vr * vr + vi * vi);
                    vr *= m;
                    vi *= m;
                }
            }
        }

        // RBJ band-pass (0 dB peak) coefficients for a transposed direct form II with b1 = 0 and b2 = -b0.
        private static void BandPass(float hz, float bandwidth, int rate, out float b0, out float a1, out float a2)
        {
            float wc = TwoPi * Math.Min(hz, rate * 0.45f) / rate;
            float alpha = MathF.Sin(wc) * Math.Max(1f, bandwidth) / (2f * Math.Max(1f, hz));
            float inv = 1f / (1f + alpha);
            b0 = alpha * inv;
            a1 = -2f * MathF.Cos(wc) * inv;
            a2 = (1f - alpha) * inv;
        }
        #endregion

        #region Space and finishing
        private static readonly float[] s_FdnMs = { 31.7f, 37.9f, 41.3f, 46.1f, 52.9f, 58.3f, 66.7f, 73.1f };
        private static readonly float[] s_TapMs = { 6.3f, 10.9f, 15.7f, 19.1f, 23.9f, 29.3f, 34.1f, 41.3f };
        private static readonly float[] s_TapGain = { 0.62f, -0.5f, 0.44f, -0.38f, 0.33f, -0.28f, 0.24f, -0.2f };

        /// <summary>
        /// A stereo room: early reflections then an eight-line feedback delay network (Householder mixing, damped lines),
        /// fed with the mid of the buffer and added to it. Strictly linear and time-invariant with fixed gains, so the
        /// overlapping tails of consecutive segments add up to exactly what one long render would have given.
        /// </summary>
        public static void Space(StereoBuf s, int rate, float rt60, float dampHz, float wet, float early, float predelay, float size = 1f)
        {
            int n = s.Length;
            float[] left = s.L, right = s.R;
            int pre = Math.Max(0, (int)(predelay * rate));
            if (early > 0f)
            {
                // Early reflections, in place without a copy: walking backwards, every tap still reads the dry signal.
                int taps = s_TapMs.Length;
                var off = new int[taps];
                var k = new float[taps];
                for (int t = 0; t < taps; t++)
                {
                    off[t] = pre + (int)(s_TapMs[t] * size * 0.001f * rate);
                    k[t] = 0.5f * early * s_TapGain[t];
                }
                for (int i = n - 1; i >= off[0]; i--)
                {
                    float el = 0f, er = 0f;
                    for (int t = 0; t < taps; t++)
                    {
                        int j = i - off[t];
                        if (j < 0) break;
                        float v = (left[j] + right[j]) * k[t];
                        if ((t & 1) == 0) el += v;
                        else er += v;
                    }
                    left[i] += el;
                    right[i] += er;
                }
            }

            int n0 = Line(0, size, rate), n1 = Line(1, size, rate), n2 = Line(2, size, rate), n3 = Line(3, size, rate);
            int n4 = Line(4, size, rate), n5 = Line(5, size, rate), n6 = Line(6, size, rate), n7 = Line(7, size, rate);
            int o1 = n0, o2 = o1 + n1, o3 = o2 + n2, o4 = o3 + n3, o5 = o4 + n4, o6 = o5 + n5, o7 = o6 + n6;
            var buf = new float[o7 + n7];
            rt60 = Math.Max(0.1f, rt60);
            float g0 = Decay(n0, rate, rt60), g1 = Decay(n1, rate, rt60), g2 = Decay(n2, rate, rt60), g3 = Decay(n3, rate, rt60);
            float g4 = Decay(n4, rate, rt60), g5 = Decay(n5, rate, rt60), g6 = Decay(n6, rate, rt60), g7 = Decay(n7, rate, rt60);
            float energy = (g0 * g0 + g1 * g1 + g2 * g2 + g3 * g3 + g4 * g4 + g5 * g5 + g6 * g6 + g7 * g7) / 8f;
            float inScale = MathF.Sqrt(Math.Max(0.01f, 1f - energy));
            float lpA = MathF.Exp(-TwoPi * Math.Min(dampHz, rate * 0.45f) / rate), wetK = wet * 0.7f;
            int i0 = 0, i1 = 0, i2 = 0, i3 = 0, i4 = 0, i5 = 0, i6 = 0, i7 = 0;
            float z0 = 0f, z1 = 0f, z2 = 0f, z3 = 0f, z4 = 0f, z5 = 0f, z6 = 0f, z7 = 0f;
            // The pre-delay: a short ring of the recent mid (the buffer itself takes the wet signal as it goes).
            var ring = new float[Math.Max(1, pre)];
            int rj = 0;
            for (int i = 0; i < n; i++)
            {
                float m = 0.5f * (left[i] + right[i]), x;
                if (pre > 0)
                {
                    x = ring[rj] * inScale;
                    ring[rj] = m;
                    if (++rj == pre) rj = 0;
                }
                else x = m * inScale;
                float o = buf[i0];
                z0 = o + (z0 - o) * lpA;
                o = buf[o1 + i1];
                z1 = o + (z1 - o) * lpA;
                o = buf[o2 + i2];
                z2 = o + (z2 - o) * lpA;
                o = buf[o3 + i3];
                z3 = o + (z3 - o) * lpA;
                o = buf[o4 + i4];
                z4 = o + (z4 - o) * lpA;
                o = buf[o5 + i5];
                z5 = o + (z5 - o) * lpA;
                o = buf[o6 + i6];
                z6 = o + (z6 - o) * lpA;
                o = buf[o7 + i7];
                z7 = o + (z7 - o) * lpA;
                float y0 = z0 * g0, y1 = z1 * g1, y2 = z2 * g2, y3 = z3 * g3, y4 = z4 * g4, y5 = z5 * g5, y6 = z6 * g6, y7 = z7 * g7;
                float sum = (y0 + y1 + y2 + y3 + y4 + y5 + y6 + y7) * 0.25f;
                buf[i0] = y0 - sum + x;
                if (++i0 == n0) i0 = 0;
                buf[o1 + i1] = y1 - sum - x;
                if (++i1 == n1) i1 = 0;
                buf[o2 + i2] = y2 - sum + x;
                if (++i2 == n2) i2 = 0;
                buf[o3 + i3] = y3 - sum - x;
                if (++i3 == n3) i3 = 0;
                buf[o4 + i4] = y4 - sum + x;
                if (++i4 == n4) i4 = 0;
                buf[o5 + i5] = y5 - sum - x;
                if (++i5 == n5) i5 = 0;
                buf[o6 + i6] = y6 - sum + x;
                if (++i6 == n6) i6 = 0;
                buf[o7 + i7] = y7 - sum - x;
                if (++i7 == n7) i7 = 0;
                left[i] += wetK * (y0 + y1 - y2 - y3 + y4 + y5 - y6 - y7);
                right[i] += wetK * (y0 - y1 + y2 - y3 + y4 - y5 + y6 - y7);
            }
        }

        private static int Line(int k, float size, int rate) => Math.Max(8, (int)(s_FdnMs[k] * size * 0.001f * rate));

        private static float Decay(int len, int rate, float rt60) => MathF.Pow(10f, -3f * len / (rate * rt60));

        /// <summary>A mono room for the jukebox's records (four lines; cheaper, and a record's room is small).</summary>
        public static void Room(float[] x, int rate, float rt60, float dampHz, float wet)
        {
            var s = new StereoBuf((float[])x.Clone(), (float[])x.Clone());
            Space(s, rate, rt60, dampHz, wet, 0f, 0.006f, 0.55f);
            for (int i = 0; i < x.Length; i++) x[i] = 0.5f * (s.L[i] + s.R[i]);
        }

        /// <summary>
        /// A stereo-linked compressor with its gain computed every 16 samples (cheap enough to finish every stinger):
        /// above <paramref name="thresholdDb"/> the level is reduced by <paramref name="ratio"/>.
        /// </summary>
        public static void Compress(StereoBuf s, int rate, float thresholdDb, float ratio, float attackMs, float releaseMs)
        {
            float att = MathF.Exp(-1f / Math.Max(1f, attackMs * 0.001f * rate));
            float rel = MathF.Exp(-1f / Math.Max(1f, releaseMs * 0.001f * rate));
            float slope = 1f - 1f / Math.Max(1f, ratio), threshold = MathF.Pow(10f, thresholdDb / 20f);
            float[] left = s.L, right = s.R;
            float env = 0f, gain = 1f, target = 1f, step = 0f;
            for (int i = 0; i < left.Length; i++)
            {
                float a = Math.Max(Math.Abs(left[i]), Math.Abs(right[i]));
                env = a > env ? att * env + (1f - att) * a : rel * env + (1f - rel) * a;
                if ((i & 15) == 0)
                {
                    target = env > threshold ? MathF.Pow(env / threshold, -slope) : 1f;
                    step = (target - gain) / 16f;
                }
                gain += step;
                left[i] *= gain;
                right[i] *= gain;
            }
        }

        /// <summary>Static soft limiting above <paramref name="knee"/> (memoryless: a safety net, not a compressor).</summary>
        public static void SoftLimit(float[] x, float knee = 0.8f)
        {
            float span = 1f - knee;
            for (int i = 0; i < x.Length; i++)
            {
                float v = x[i], a = v < 0f ? -v : v;
                if (a <= knee) continue;
                float o = knee + span * MathF.Tanh((a - knee) / span);
                x[i] = v < 0f ? -o : o;
            }
        }

        /// <summary>Fades the last <paramref name="seconds"/> of both sides to silence (the cut-off end of a ring-out).</summary>
        public static void FadeEnd(StereoBuf s, int rate, float seconds)
        {
            int n = Math.Min(s.Length, (int)(seconds * rate));
            for (int i = 0; i < n; i++)
            {
                float w = i / (float)n;
                w *= w;
                int j = s.Length - 1 - i;
                s.L[j] *= w;
                s.R[j] *= w;
            }
        }

        /// <summary>The two sides interleaved, as an AudioClip wants them.</summary>
        public static float[] Interleave(StereoBuf s)
        {
            var o = new float[s.Length * 2];
            for (int i = 0; i < s.Length; i++)
            {
                o[2 * i] = s.L[i];
                o[2 * i + 1] = s.R[i];
            }
            return o;
        }

        /// <summary>A stable 32-bit mix of a seed and an index (splitmix-style), for per-segment generators.</summary>
        public static uint Hash(uint a, long b, uint salt = 0u)
        {
            ulong z = a * 0x9E3779B97F4A7C15ul ^ (ulong)b * 0xBF58476D1CE4E5B9ul ^ salt * 0x94D049BB133111EBul;
            z ^= z >> 30;
            z *= 0xBF58476D1CE4E5B9ul;
            z ^= z >> 27;
            z *= 0x94D049BB133111EBul;
            z ^= z >> 31;
            uint r = (uint)(z ^ (z >> 32));
            return r == 0u ? 0x6D2B79F5u : r;
        }

        /// <summary>A generator for one piece of the score.</summary>
        public static SynthGen Gen(int rate, uint seed, long index, uint salt) => new SynthGen(rate, Hash(seed, index, salt), 0);
        #endregion
    }
}
